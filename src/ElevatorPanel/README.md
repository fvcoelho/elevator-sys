# ElevatorPanel

A console application for submitting elevator requests to the multi-elevator system via a shared file.

## Overview

ElevatorPanel allows you to submit elevator ride requests that are automatically picked up and processed by the main ElevatorSystem application. This enables:
- External request submission
- Automated testing scenarios
- Load testing
- Request logging and history

## Usage

### Interactive Mode

Run the application without arguments to enter interactive mode:

```bash
cd /Users/fvcoelho/Working/elevator-sys
dotnet run --project src/ElevatorPanel/ElevatorPanel.csproj
```

You'll see:

```
=== ELEVATOR PANEL REQUEST ===

Enter elevator requests to send to the system.
Requests are written to: /Users/fvcoelho/Working/elevator-sys/elevator_requests.txt

[A] Add Request
[Q] Quit

>
```

Press `A` to add a request, then enter:
1. Pickup floor (1-20)
2. Destination floor (1-20)

The request will be written to `elevator_requests.txt` and picked up by the elevator system within 500ms.

### Command Line Mode

Submit a single request directly:

```bash
dotnet run --project src/ElevatorPanel/ElevatorPanel.csproj -- <pickup> <destination>
```

Example:
```bash
dotnet run --project src/ElevatorPanel/ElevatorPanel.csproj -- 5 15
# Output: ✓ Request added: 5 → 15
```

### Running with Elevator System

For the full experience, run both applications simultaneously:

**Terminal 1 - Elevator System:**
```bash
cd /Users/fvcoelho/Working/elevator-sys
dotnet run --project src/ElevatorSystem/ElevatorSystem.csproj
```

**Terminal 2 - Request Writer:**
```bash
cd /Users/fvcoelho/Working/elevator-sys
dotnet run --project src/ElevatorPanel/ElevatorPanel.csproj
```

Add requests in Terminal 2 and watch them being processed in Terminal 1!

## Validation

The application validates:
- Floor numbers must be between 1 and 20
- Pickup floor must differ from destination floor
- Input must be numeric

Invalid requests are rejected with clear error messages.

## File Format

Requests are written to `elevator_requests.txt` in the format:

```
pickup destination
```

Each line contains one request with pickup and destination floors separated by a space.

Example file contents:
```
5 15
3 10
8 2
```

## Features

- ✅ Interactive console interface
- ✅ Command line arguments for automation
- ✅ Input validation
- ✅ Clear error messages
- ✅ File append mode (preserves history)
- ✅ Works concurrently with elevator system
- ✅ Handles file locking gracefully

## Building

```bash
dotnet build src/ElevatorPanel/ElevatorPanel.csproj
```

## Technical Details

- **Target Framework:** .NET 8
- **File Path:** `elevator_requests.txt` (relative to working directory)
- **File Mode:** Append (preserves existing requests)
- **Thread Safety:** File I/O with proper exception handling
