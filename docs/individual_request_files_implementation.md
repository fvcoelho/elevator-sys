# Individual Request Files Implementation

**Date**: 2026-02-23
**Status**: ✅ Completed

## Overview

Successfully redesigned the RequestWriter and ElevatorSystem to use **individual request files** instead of a single shared file. Each request is now written to its own timestamped file in a `requests/` directory, processed by the ElevatorSystem, and archived to a `processed/` directory.

## What Changed

### Architecture

**OLD (Single File)**:
```
elevator_requests.txt
  5 10
  3 15 # waiting
  7 20 # doing
```

**NEW (Individual Files)**:
```
requests/
  20260223_214530_from_5_to_15.txt   ← content: "5 15"
  20260223_214532_from_10_to_3.txt   ← content: "10 3"
processed/
  20260223_214520_from_3_to_8.txt    ← archived after processing
```

### File Naming Pattern

**Format**: `{timestamp}_from_{pickup}_to_{destination}.txt`

**Example**: `20260223_214530_from_5_to_15.txt`
- Timestamp: `yyyyMMdd_HHmmss` format (year-month-day_hour-minute-second)
- Pickup floor: `5`
- Destination floor: `15`
- Content: `5 15` (pickup destination on single line)

### File Lifecycle

1. **RequestWriter** creates file in `requests/` → **Pending**
2. **ElevatorSystem** detects and processes file → **Processing**
3. **ElevatorSystem** moves file to `processed/` → **Done (Archived)**

## Benefits

✅ **No file contention** - Each request is independent
✅ **Atomic operations** - File creation = new request
✅ **Simpler status tracking** - File location = status (requests/ vs processed/)
✅ **Better concurrency** - Multiple writers can work simultaneously
✅ **Easier debugging** - One request per file, clear audit trail
✅ **Natural ordering** - Timestamp in filename provides sequence
✅ **Audit trail** - All processed requests archived in processed/ directory
✅ **No inline updates** - Files are created once, moved once (no rewrites)

## Implementation Details

### RequestWriter Changes (`src/RequestWriter/Program.cs`)

**Lines Modified**: 5-7 (constants), 11-17 (directory creation), 38 (welcome message), 102-107 (output), 154-175 (WriteRequest method)

**Key Changes**:
1. Changed `REQUEST_FILE` constant to `REQUESTS_DIR = "requests"`
2. Added directory creation on startup
3. Modified `WriteRequest()` to:
   - Generate timestamp using `DateTime.Now.ToString("yyyyMMdd_HHmmss")`
   - Create filename: `{timestamp}_from_{pickup}_to_{destination}.txt`
   - Write request to individual file using `File.WriteAllText()`
   - Return filename (or null on error)
4. Updated console output to show created filename

### ElevatorSystem Changes (`src/ElevatorSystem/Program.cs`)

**Lines Modified**: 9-10 (constants), 19-32 (initialization), 51-187 (entire file monitoring task), 194 (console output)

**Key Changes**:
1. Added constants:
   - `REQUESTS_DIR = "requests"` - Directory for pending requests
   - `PROCESSED_DIR = "processed"` - Directory for processed requests
2. Created both directories on startup
3. Replaced `processedLines` HashSet with `processedFiles` HashSet (tracks filenames)
4. Removed status tracking code:
   - `requestIdToLineContent` dictionary (no longer needed)
   - `fileLock` object (no longer needed)
   - Status update logic (# waiting → # doing → # done)
5. Completely rewrote file monitoring task:
   - Monitors `requests/` directory for `.txt` files
   - Parses filename to extract pickup and destination
   - Creates `Request` object and adds to system
   - Moves processed file to `processed/` directory
   - Handles invalid filenames and floor numbers gracefully
6. Updated console output to show both directories

### Filename Parsing Logic

```csharp
// Example: "20260223_214530_from_5_to_15.txt"
var nameWithoutExt = filename.Replace(".txt", "");
var parts = nameWithoutExt.Split('_');

// Expected parts:
// [0] = "20260223"    (date)
// [1] = "214530"      (time)
// [2] = "from"        (separator)
// [3] = "5"           ← pickup floor
// [4] = "to"          (separator)
// [5] = "15"          ← destination floor

if (parts.Length >= 6 && parts[2] == "from" && parts[4] == "to")
{
    if (int.TryParse(parts[3], out int pickup) &&
        int.TryParse(parts[5], out int destination))
    {
        // Valid request - process it
    }
}
```

### Error Handling

**Invalid filename format**: Logged and skipped (added to processedFiles to prevent retries)
**Invalid floor numbers**: Logged and skipped
**File locked**: Error logged, retry on next iteration (500ms later)
**Out-of-range floors**: Caught by Request constructor, logged and skipped

## Testing

### Test Results

✅ **Build**: Successful (0 warnings, 0 errors)
✅ **Unit Tests**: All 64 tests pass
✅ **Integration Tests**: Manual testing confirms complete workflow

### Manual Integration Test

```bash
# Terminal 1: Start ElevatorSystem
dotnet run --project src/ElevatorSystem/ElevatorSystem.csproj

# Terminal 2: Add requests via RequestWriter
dotnet run --project src/RequestWriter/RequestWriter.csproj -- 5 15
dotnet run --project src/RequestWriter/RequestWriter.csproj -- 10 3
dotnet run --project src/RequestWriter/RequestWriter.csproj -- 8 20

# Result: Files appear in requests/, get processed, move to processed/
```

### Verified Behaviors

✅ RequestWriter creates files in `requests/` with correct naming
✅ ElevatorSystem detects files within 500ms
✅ Requests are parsed and added to system
✅ Files are moved from `requests/` to `processed/`
✅ File content is preserved (not modified)
✅ Invalid filenames are handled gracefully
✅ Out-of-range floors are rejected
✅ Multiple requests can be processed concurrently
✅ Audit trail is preserved in `processed/` directory

## Edge Cases Handled

✅ **Timestamp collisions**: Very rare (1-second granularity), could add milliseconds if needed
✅ **Invalid filename format**: Logged and skipped (e.g., `invalid_filename.txt`)
✅ **Invalid floor numbers**: Logged and skipped (e.g., `20260223_230000_from_99_to_3.txt`)
✅ **Missing directories**: Auto-created on startup
✅ **File locked**: Retry on next iteration (500ms polling)
✅ **Empty requests directory**: No errors, just polls every 500ms

## Migration Notes

### Old System (`elevator_requests.txt`)

- **Status**: Deprecated, no longer used
- **Migration**: No action required - new system only monitors `requests/` directory
- **Backward compatibility**: Not needed - clean break

### Existing Code

- **Internal tracking**: Unchanged (RequestProgress, active requests still work)
- **Elevator.cs**: Unchanged (same thread-safe implementation)
- **Request.cs**: Unchanged (same validation and direction logic)
- **Tests**: All 64 tests pass without modification

## Directory Structure

```
elevator-sys/
  requests/                       ← NEW: Pending requests (monitored)
    20260223_214530_from_5_to_15.txt
    20260223_214532_from_10_to_3.txt
  processed/                      ← NEW: Archived requests (audit trail)
    20260223_214520_from_3_to_8.txt
    20260223_214525_from_12_to_1.txt
  src/
    ElevatorSystem/
      Program.cs                  ← Modified: Monitors requests/ directory
    RequestWriter/
      Program.cs                  ← Modified: Creates files in requests/
  tests/
    ElevatorSystem.Tests/         ← Unchanged: All tests pass
```

## Commands

```bash
# Build the project
dotnet build

# Run tests (64 tests)
dotnet test

# Run ElevatorSystem
dotnet run --project src/ElevatorSystem/ElevatorSystem.csproj

# Run RequestWriter (interactive mode)
dotnet run --project src/RequestWriter/RequestWriter.csproj

# Run RequestWriter (command line mode)
dotnet run --project src/RequestWriter/RequestWriter.csproj -- <pickup> <destination>
# Example: dotnet run --project src/RequestWriter/RequestWriter.csproj -- 5 15
```

## Future Enhancements

- **Failed requests**: Move invalid files to `failed/` directory (separate from processed/)
- **Timestamp collision handling**: Add milliseconds or sequential counter if needed
- **Batch processing**: Process multiple files in parallel for high throughput
- **File watching**: Use FileSystemWatcher instead of polling (more efficient)
- **Request metadata**: Add priority, user ID to filename pattern
- **Archive cleanup**: Automatic cleanup of old files in processed/ (e.g., delete after 7 days)
- **Statistics**: Track request volume, processing time from processed/ files

## Summary

The implementation successfully replaced the single shared file approach with individual timestamped request files. This provides better concurrency, simpler status tracking, and a complete audit trail. All tests pass, and manual integration testing confirms the complete workflow works correctly.

**Files Modified**: 2
**Files Created**: 0 (directories created at runtime)
**Files Removed**: 0 (elevator_requests.txt deprecated but not deleted)
**Tests**: 64/64 passing ✅
**Build**: Successful ✅
