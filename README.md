# Elevator Control System (Hard Level)

A .NET 8 console application simulating a multi-elevator system with 3-5 configurable elevators serving floors 1-20, featuring intelligent dispatch, load balancing, and thread-safe concurrent operations.

## Features

- **Multi-Elevator System**: 3-5 configurable elevators (default: 3)
- **Floors 1-20** (configurable)
- **Intelligent Dispatch**: Idle preference + closest distance, with SCAN and LOOK algorithms
- **Elevator Types**: Standard, Express, Freight with configurable capacity and speed
- **Floor Access Control**: Restrict elevator access to specific floors
- **Request Priority**: Normal and High priority levels
- **Performance Analytics**: Real-time metrics tracking
- **Complete Ride Requests**: Pickup floor → Destination floor
- **Thread-Safe**: `ConcurrentQueue` for lock-free request handling, locks for state management
- **134 unit and integration tests**

## Getting Started

### Prerequisites

- Git
- .NET 8 SDK or higher

### Clone & Build

```bash
git clone git@github.com:fvcoelho/elevator-sys.git
cd elevator-sys
dotnet build
dotnet test
```

## Running the System on Mac

Open separate Terminal windows (or use iTerm2 split panes) for each component:

```bash
# Terminal 1: Main application
dotnet run --project src/ElevatorSystem/ElevatorSystem.csproj

# Terminal 2: Elevator panel (external requests)
dotnet run --project src/ElevatorPanel/ElevatorPanel.csproj

# Terminals 3-5: Log monitors
tail -f logs/elevator_A.log
tail -f logs/elevator_B.log
tail -f logs/elevator_C.log
```

Arrange terminals so logs are visible alongside the applications.

If logs directory doesn't exist or logs aren't updating:

```bash
mkdir -p logs && chmod 755 logs
```

To stop background processes:

```bash
pgrep -f "dotnet.*Elevator"
kill <PID>
```

## Running the System on Windows

Open separate Command Prompt or PowerShell windows for each component:

```powershell
# Terminal 1: Main application
dotnet run --project src\ElevatorSystem\ElevatorSystem.csproj

# Terminal 2: Elevator panel (external requests)
dotnet run --project src\ElevatorPanel\ElevatorPanel.csproj

# Terminals 3-5: Log monitors (PowerShell)
Get-Content logs\elevator_A.log -Wait
Get-Content logs\elevator_B.log -Wait
Get-Content logs\elevator_C.log -Wait
```

If logs directory doesn't exist:

```powershell
mkdir logs
```

### Controls

| Key | Action |
|-----|--------|
| **R** | Request a ride (pickup + destination) |
| **S** | View system status |
| **A** | View analytics (performance metrics) |
| **D** | Change dispatch algorithm (Simple/SCAN/LOOK) |
| **M** | Toggle elevator maintenance mode |
| **Q** | Quit |

## Architecture

```
User Request → ElevatorSystem (Dispatcher)
                     ↓
         FindBestElevator (scoring)
                     ↓
         ┌───────────┼───────────┐
    Elevator A  Elevator B  Elevator C
```

### Core Components

| File | Purpose |
|------|---------|
| `ElevatorSystem.cs` | Centralized dispatcher, request queue, dispatch algorithms |
| `Elevator.cs` | Core elevator logic, thread-safe state and movement |
| `Request.cs` | Immutable ride request model with auto-direction |
| `Program.cs` | Interactive console interface |
| `ElevatorConfig.cs` | Elevator type configuration (Standard/Express/Freight) |
| `FloorAccess.cs` | Floor access restrictions |
| `PerformanceTracker.cs` | Analytics and metrics collection |
| `ElevatorFileLogger.cs` | Per-elevator file logging |
| `DispatchAlgorithm.cs` | Simple, SCAN, and LOOK dispatch strategies |
| `RequestPriority.cs` | Normal/High priority levels |

### Dispatch Algorithm

1. If any **idle** elevators exist → pick closest idle to pickup floor
2. Otherwise → pick closest busy elevator to pickup floor
3. High priority requests bypass idle preference

SCAN and LOOK algorithms provide sweep-motion optimization for higher throughput.

### Initial Elevator Distribution

| Count | Starting Floors |
|-------|----------------|
| 3 | 1, 10, 20 |
| 4 | 1, 7, 14, 20 |
| 5 | 1, 5, 10, 15, 20 |

## Thread Safety

- **Requests**: `ConcurrentQueue<Request>` (lock-free)
- **Request IDs**: `Interlocked.Increment`
- **Dispatch scoring**: Protected by `_dispatchLock`
- **Elevator state/floor**: Lock-protected properties
- **Target floors**: `ConcurrentQueue<int>` (lock-free)
- **Processing**: Single dispatcher loop + independent per-elevator tasks

## Project Structure

```
elevator-sys/
├── src/
│   ├── ElevatorSystem/          # Main application
│   └── ElevatorPanel/          # External panel client
├── tests/
│   └── ElevatorSystem.Tests/    # 134 tests
├── logs/                        # Per-elevator log files
├── docs/                        # Specs and plans
└── ElevatorSystem.sln
```

## License

Demonstration project for educational purposes.
