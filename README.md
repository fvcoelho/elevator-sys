# Elevator Control System

A minimal .NET 8 console application simulating a single elevator serving floors 1-10 with FIFO (First-In-First-Out) scheduling and thread-safe operations.

## Features

- **Single Elevator**: Serves floors 1-10
- **FIFO Scheduling**: Requests are processed in the order they are received
- **Thread-Safe Operations**: Uses `ConcurrentQueue` for lock-free request handling and locks for state management
- **Console Interface**: Interactive command-line interface with instant key response for requesting floors
- **Comprehensive Tests**: 18 unit tests covering all functionality

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
   - Console interface for user interaction with instant key response
   - Automatically displays pending queue after operations
   - Handles user commands (floor requests via keys 1-0, Quit)

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

Press: [1-9] Floor 1-9 | [0] Floor 10 | [Q] Quit
```

### Commands

- **Keys 1-9**: Request floors 1-9 (instant, no Enter key needed)
- **Key 0**: Request floor 10
- **Q**: Shut down the elevator system

The pending request queue is automatically displayed after each elevator operation completes.

### Example Session

```
[User presses '5']
Requesting floor 5...
Request received for floor 5 → Pending: [5]
Added floor 5 → Queue: [5]
Elevator moved up to floor 5
Doors are OPEN at floor 5
Doors are CLOSED (IDLE) at floor 5
 → Pending: []

[User presses '8']
Requesting floor 8...
Request received for floor 8 → Pending: [8]
Added floor 8 → Queue: [8]
Elevator moved up to floor 8
Doors are OPEN at floor 8
Doors are CLOSED (IDLE) at floor 8
 → Pending: []
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

**Test Results**: 18/18 tests passing

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
