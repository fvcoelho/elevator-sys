# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a minimal .NET 8 elevator control system implementing a single elevator serving floors 1-10 with FIFO (First-In-First-Out) scheduling and thread-safe operations. The system features a console-based interface for requesting floors and monitoring elevator status.

## Development Setup

This project uses .NET 8, xUnit for testing, and FluentAssertions for test assertions.

### Common Commands

```bash
# Build the project
dotnet build

# Run tests (19 tests)
dotnet test

# Run the application
dotnet run --project src/ElevatorSystem

# Build in Release mode
dotnet build --configuration Release
```

## Architecture

The system uses a simple, straightforward architecture with no dependency injection or complex frameworks - just clear, maintainable code.

### Core Components

- **Elevator** (`src/ElevatorSystem/Elevator.cs`): Core elevator logic with thread-safe state management using locks and `ConcurrentQueue` for FIFO target handling
- **ElevatorController** (`src/ElevatorSystem/ElevatorController.cs`): Orchestrates request processing with a single background processing loop
- **Program** (`src/ElevatorSystem/Program.cs`): Interactive console interface for user commands (Request, Status, Quit)
- **Enums**: `ElevatorState` (IDLE, MOVING_UP, MOVING_DOWN, DOOR_OPEN) and `Direction` (NONE, UP, DOWN)

### Key Considerations

- **Concurrency**: Elevator systems require careful handling of concurrent requests
- **Safety**: Door operations, weight limits, and emergency protocols must be prioritized
- **Scheduling Algorithms**: Choose efficient algorithms to minimize wait times
- **Real-time Updates**: System state should be reflected accurately in real-time

## Coding Standards

- **C#/.NET 8**: Main language for this project
- **Thread Safety**: Use `ConcurrentQueue<T>` for lock-free queues and `lock` for state synchronization
- **Error Handling**: Validate floor ranges and throw appropriate exceptions
- **Testing**: Maintain comprehensive unit test coverage (currently 19/19 tests passing)
- **Async/Await**: Use async methods for I/O and delays
- **Console Output**: Use `Console.WriteLine` for status updates and logging

## Implementation Details

- **Scheduling**: Pure FIFO - requests processed strictly in order received
- **Configuration**: Floors 1-10, 2s door open time, 1.5s per floor travel time
- **Thread Safety Strategy**:
  - `Elevator.CurrentFloor` and `Elevator.State`: Lock-protected
  - Target floors and request queues: `ConcurrentQueue<int>` (lock-free)
  - Single processing loop thread
- **No optimization**: Elevator doesn't combine nearby requests or optimize routes
