# Elevator Control System

A minimal .NET 8 console application simulating a single elevator serving floors 1-10 with FIFO (First-In-First-Out) scheduling and thread-safe operations.

## Features

- **Single Elevator**: Serves floors 1-10
- **FIFO Scheduling**: Requests are processed in the order they are received
- **Thread-Safe Operations**: Uses `ConcurrentQueue` for lock-free request handling and locks for state management
- **Console Interface**: Interactive command-line interface for requesting floors and viewing status
- **Comprehensive Tests**: 19 unit tests covering all functionality

## Architecture

### Core Components

1. **Elevator** (`Elevator.cs`)
   - Manages elevator state, current floor, and movement
   - Thread-safe using locks for state/floor and `ConcurrentQueue` for targets
   - Async movement methods with configurable delays

2. **ElevatorController** (`ElevatorController.cs`)
   - Orchestrates request processing
   - Manages request queue and elevator coordination
   - Single background processing loop

3. **Program** (`Program.cs`)
   - Console interface for user interaction
   - Displays real-time status
   - Handles user commands (Request, Status, Quit)

### Configuration

- **Floors**: 1-10
- **Initial Floor**: 1
- **Door Open Time**: 2 seconds
- **Floor Travel Time**: 1.5 seconds per floor

## Getting Started

### Prerequisites

- .NET 8 SDK or higher

### Building

```bash
# Build the solution
dotnet build

# Build in Release mode
dotnet build --configuration Release
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal
```

### Running the Application

```bash
# Run from project directory
dotnet run --project src/ElevatorSystem

# Or from the project folder
cd src/ElevatorSystem
dotnet run
```

## Usage

Once the application starts, you'll see an interactive menu with instant key response:

```
=== ELEVATOR SYSTEM STARTED ===

Press keys [1-9] for floors 1-9, [0] for floor 10, [S] for status, [Q] to quit

========================================
Current Floor: 1
State: IDLE
Target Queue: []
Pending Requests: 0
========================================

Press: [1-9] Floor 1-9 | [0] Floor 10 | [S] Status | [Q] Quit
```

### Commands

- **Keys 1-9**: Request floors 1-9 (instant, no Enter key needed)
- **Key 0**: Request floor 10
- **S**: Refresh status display
- **Q**: Shut down the elevator system

### Example Session

```
[User presses '5']
Requesting floor 5...
Request received for floor 5
Added floor 5 to elevator target queue

[User presses '8']
Requesting floor 8...
Request received for floor 8

[User presses 'S']
[Refreshing status...]

========================================
Current Floor: 5
State: DOOR_OPEN
Target Queue: [8]
Pending Requests: 0
========================================
```

## Testing

The project includes comprehensive unit tests:

### Elevator Tests (`ElevatorTests.cs`)
- Movement (up/down) functionality
- Boundary conditions (top/bottom floors)
- Door operations (open/close)
- FIFO queue behavior
- Input validation

### Controller Tests (`ElevatorControllerTests.cs`)
- Request handling
- Request validation
- Integration with elevator
- Concurrent request processing (20 concurrent requests)

**Test Results**: 19/19 tests passing

## Design Decisions

1. **No Dependency Injection**: Simple, manual instance creation for clarity
2. **ConcurrentQueue**: Lock-free FIFO queues for requests and targets
3. **Lock-based State Management**: Simple synchronization for current floor and state
4. **Single Processing Loop**: One background task for easier reasoning
5. **Async/Await**: Natural delays for simulation, non-blocking operations
6. **Pure FIFO**: Requests processed strictly in order received (no optimization)

## Thread Safety

- **Elevator.CurrentFloor**: Lock-protected property
- **Elevator.State**: Lock-protected property
- **Elevator._targetFloors**: `ConcurrentQueue<int>` (lock-free)
- **Controller._requestQueue**: `ConcurrentQueue<int>` (lock-free)
- **Processing Loop**: Single-threaded (only one `ProcessRequestsAsync` call)

## Project Structure

```
elevator-sys/
├── src/
│   └── ElevatorSystem/
│       ├── ElevatorSystem.csproj
│       ├── Program.cs              # Console interface
│       ├── ElevatorState.cs        # State enum
│       ├── Direction.cs            # Direction enum
│       ├── Elevator.cs             # Core elevator logic
│       └── ElevatorController.cs   # Request orchestration
├── tests/
│   └── ElevatorSystem.Tests/
│       ├── ElevatorSystem.Tests.csproj
│       ├── ElevatorTests.cs
│       └── ElevatorControllerTests.cs
├── ElevatorSystem.sln
├── CLAUDE.md
└── README.md
```

## Future Enhancements

Potential improvements for future iterations:

- Multiple elevator support
- Smart scheduling algorithms (SCAN, LOOK, nearest-first)
- Request deduplication
- Web-based UI
- Real-time monitoring dashboard
- Emergency stop functionality
- Weight/capacity limits
- Logging infrastructure

## License

This is a demonstration project for educational purposes.
