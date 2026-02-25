# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

.NET 8 multi-elevator control system with 3-5 configurable elevators serving floors 1-20. Features intelligent dispatch (Simple/SCAN/LOOK algorithms), elevator types (Local/Express/Freight), VIP floor access, performance analytics, maintenance mode, emergency stop, and file-based request input from a separate panel app.

## Commands

```bash
# Build
dotnet build

# Run all tests (168 tests, ~7s)
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~ElevatorSystemTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName~ElevatorSystemTests.FindBestElevator_SelectsIdleOverBusy"

# Run the main application
dotnet run --project src/ElevatorSystem

# Run the external panel (writes request files for the main app to consume)
dotnet run --project src/ElevatorPanel

# Panel CLI mode (single request)
dotnet run --project src/ElevatorPanel -- 3 15

# Panel traffic generation
dotnet run --project src/ElevatorPanel -- --rush    # or --light, --moderate
```

## Architecture

**Centralized dispatcher pattern** — no DI, no frameworks.

```
ElevatorPanel (writes .txt files to requests/)
        ↓
ElevatorSystem (file monitor + dispatcher)
        ↓
FindBestElevator (scoring by algorithm)
        ↓
    ┌───┼───┬───┐
  Elev A  B  C  F1   (independent async tasks)
```

Two communicating processes: **ElevatorSystem** (main app) monitors a `requests/` directory for `.txt` files written by **ElevatorPanel**. Processed files are moved to `processed/`.

### Request file format
`{timestamp}_from_{pickup}_to_{destination}[_{flag}].txt` where flag is `H` (high priority), `V` (VIP), or `F` (freight).

### Core source files (all in `src/ElevatorSystem/`)

| File | Purpose |
|------|---------|
| `ElevatorSystem.cs` | Dispatcher: request queue, dispatch algorithms (Simple/SCAN/LOOK), elevator lifecycle, floor restrictions, performance tracking |
| `Elevator.cs` | Single elevator: thread-safe state, async movement, door operations, maintenance mode, emergency stop |
| `Request.cs` | Immutable ride request with auto-direction, priority, access level, preferred elevator type |
| `Program.cs` | Console UI + file monitor loop. Configures system profile (Standard/Mixed/Full) and VIP floors |
| `ElevatorConfig.cs` | Per-elevator config: label, initial floor, type, served floors, capacity |
| `FloorAccess.cs` | Floor restriction rules (VIPOnly) checked during dispatch |
| `PerformanceTracker.cs` / `PerformanceMetrics.cs` | Analytics: wait/ride/dispatch times, utilization, floor heatmap |
| `ElevatorFileLogger.cs` | Per-elevator file logging to `logs/` directory |

### Enums
- `ElevatorState`: IDLE, MOVING_UP, MOVING_DOWN, DOOR_OPENING, DOOR_OPEN, DOOR_CLOSING, MAINTENANCE, EMERGENCY_STOP
- `ElevatorType`: Local, Express, Freight
- `DispatchAlgorithm`: Simple, SCAN, LOOK
- `RequestPriority`: Normal, High
- `Direction`: NONE, UP, DOWN

### System profiles (configured in Program.cs via `PROFILE` constant)
- **Standard**: 3 Local elevators (floors 1, 10, 20)
- **Mixed**: 2 Local + 1 Express (lobby + floors 15-20)
- **Full**: 2 Local + 1 Express + 1 Freight (capacity 20)

### Console controls
`S` status | `A` analytics | `D` change dispatch algorithm | `M` toggle maintenance | `SPACE` emergency stop | `Q` quit

## Thread Safety

- `ConcurrentQueue<Request>` for incoming requests (lock-free)
- `ConcurrentQueue<int>` for per-elevator target floors (lock-free)
- `lock` on `CurrentFloor` and `State` properties in `Elevator.cs`
- `Interlocked.Increment` for request ID generation
- `_dispatchLock` protects `FindBestElevator` scoring
- Single dispatcher loop + independent per-elevator async tasks

## Tests (168 total)

| File | Count | Focus |
|------|-------|-------|
| `ElevatorSystemTests.cs` | 34 | Initialization, dispatch, integration, concurrency |
| `FloorAccessTests.cs` | 22 | VIP restrictions, access control |
| `ElevatorTypeTests.cs` | 19 | Local/Express/Freight behavior |
| `RequestTests.cs` | 18 | Validation, direction, thread-safe IDs, priority |
| `PerformanceMetricsTests.cs` | 15 | Analytics, utilization, heatmaps |
| `FeatureInteractionTests.cs` | 14 | Cross-feature integration |
| `DispatchAlgorithmTests.cs` | 12 | Simple/SCAN/LOOK scoring |
| `ElevatorTests.cs` | 9 | Movement, doors, FIFO, boundaries |
| `EmergencyStopTests.cs` | 8 | Emergency stop/resume |
| `MaintenanceModeTests.cs` | 8 | Maintenance enter/exit |

Tests use fast timings (`doorOpenMs: 5-10`, `floorTravelMs: 5-10`). Helper `WaitForSystemIdle()` polls until all elevators are IDLE with no targets.

## Coding Standards

- Thread-safe primitives: `ConcurrentQueue<T>`, `lock`, `Interlocked`
- Console output prefixes: `[SYSTEM]`, `[DISPATCH]`, `[ELEVATOR n]`, `[FILE]`, `[CONFIG]`
- Async/await for all elevator operations and delays
- Validate floor ranges and elevator counts; throw `ArgumentException` on invalid input
- If it's a `.tsx` file, always use shadcn components
