# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 8 **multi-elevator control system** implementing **3-5 configurable elevators** serving floors 1-20 with **intelligent automatic dispatch**, load balancing, and thread-safe concurrent operations. The system features a console-based interface for requesting rides (pickup + destination) and monitoring real-time system status.

## Development Setup

This project uses .NET 8, xUnit for testing, and FluentAssertions for test assertions.

### Common Commands

```bash
# Build the project
dotnet build

# Run tests (72 tests)
dotnet test

# Run the application
dotnet run --project src/ElevatorSystem

# Build in Release mode
dotnet build --configuration Release

# Shows memory
top -l 1 -s 0 | grep -i ElevatorSystem
top -pid $(pgrep -f ElevatorSystem)
```

## Architecture

The system uses a **centralized dispatcher pattern** with no dependency injection or complex frameworks - just clear, maintainable code.

### Centralized Dispatcher Pattern

```
User Request → ElevatorSystem (Dispatcher)
                     ↓
    FindBestElevator (scoring algorithm)
                     ↓
    Assign to optimal elevator
                     ↓
         ┌──────────┼──────────┬──────────┐
    Elevator A  Elevator B  Elevator C  ...
    (Task 1)    (Task 2)    (Task 3)
```

### Core Components

1. **Request** (`src/ElevatorSystem/Request.cs`)
   - Immutable ride request model (pickup floor, destination floor, direction, timestamp, ID)
   - Thread-safe ID generation with `Interlocked.Increment`
   - Auto-calculates direction (UP/DOWN) from pickup and destination
   - Validates floor ranges in constructor

2. **ElevatorSystem** (`src/ElevatorSystem/ElevatorSystem.cs`)
   - Centralized dispatcher managing 3-5 elevator instances
   - `ConcurrentQueue<Request>` for incoming requests
   - Intelligent dispatch algorithm: `FindBestElevator(Request)` with idle preference + closest distance
   - `ProcessRequestsAsync()`: Main dispatcher loop dequeues requests and assigns to elevators
   - `ProcessElevatorAsync()`: Per-elevator task processes movement and door operations
   - `GetSystemStatus()`: Real-time status of all elevators and pending requests

3. **Elevator** (`src/ElevatorSystem/Elevator.cs`)
   - Core elevator logic with thread-safe state management
   - Uses locks for `CurrentFloor` and `State`, `ConcurrentQueue<int>` for target floors
   - **Unchanged from single elevator version** - proven thread-safe, works perfectly for multi-elevator
   - Async movement methods (`MoveUp`, `MoveDown`, `OpenDoor`, `CloseDoor`)
   - `AddRequest(floor)` enqueues both pickup and destination floors

4. **Program** (`src/ElevatorSystem/Program.cs`)
   - Interactive console interface: [R] Request ride, [S] View status, [Q] Quit
   - Prompts for pickup floor and destination floor
   - Displays real-time status of all elevators

5. **Enums**
   - `ElevatorState`: IDLE, MOVING_UP, MOVING_DOWN, DOOR_OPEN
   - `Direction`: NONE, UP, DOWN (now actively used in Request)

### Key Considerations

- **Concurrency**: Multi-elevator systems require careful coordination and thread-safe request handling
- **Dispatch Strategy**: Idle preference + closest distance ensures efficient elevator utilization
- **Safety**: Door operations, state transitions, and floor validation must be thread-safe
- **Load Balancing**: Evenly distributed initial positions and intelligent dispatch spread load across elevators
- **Real-time Updates**: System state accurately reflects all elevators, targets, and pending requests

## Coding Standards

- **C#/.NET 8**: Main language for this project
- **Thread Safety**:
  - Use `ConcurrentQueue<T>` for lock-free queues (requests, targets)
  - Use `lock` for state synchronization (CurrentFloor, State)
  - Use `Interlocked` for thread-safe counters (Request IDs)
  - Protect dispatch scoring with `_dispatchLock`
- **Error Handling**: Validate floor ranges and elevator counts, throw appropriate exceptions
- **Testing**: Maintain comprehensive test coverage (currently 72/72 tests passing)
  - Unit tests for Request, ElevatorSystem, Elevator
  - Integration tests for end-to-end request processing
  - Concurrency tests (100+ concurrent requests)
- **Async/Await**: Use async methods for elevator operations and delays
- **Console Output**: Use `Console.WriteLine` with prefixes: `[SYSTEM]`, `[DISPATCH]`, `[ELEVATOR n]`

## Implementation Details

### Configuration (in Program.cs)

```csharp
const int ELEVATOR_COUNT = 3;        // Configurable: 3-5 elevators
const int MIN_FLOOR = 1;
const int MAX_FLOOR = 20;            // Extended from 10 to 20
const int DOOR_OPEN_MS = 3000;       // 3 seconds
const int FLOOR_TRAVEL_MS = 1500;    // 1.5 seconds per floor
```

### Dispatch Algorithm

**Simple + Idle Preference Strategy:**

1. Filter elevators into IDLE and BUSY groups
2. If any IDLE elevators exist, pick closest idle elevator to pickup floor
3. Otherwise (all busy), pick closest busy elevator to pickup floor
4. Distance = `|currentFloor - pickupFloor|`

**Why this works:**
- Idle elevators can respond immediately
- Closest distance minimizes passenger wait time
- Simple O(n) algorithm where n = elevator count (max 5)

### Initial Elevator Distribution

For better coverage, elevators start at evenly distributed floors:
- 3 elevators: Floors 1, 10, 20
- 4 elevators: Floors 1, 7, 14, 20
- 5 elevators: Floors 1, 5, 10, 15, 20

### How Rides Work

Each ride request specifies pickup and destination:
- User at floor 3 wants to go to floor 5 → `Request(pickup=3, dest=5, dir=UP)`
- System assigns to best elevator
- Elevator queue: `[..., 3, 5]` (pickup first, then destination)
- Elevator moves to 3 → opens doors (pickup passenger) → moves to 5 → opens doors (drop off)

### Thread Safety Strategy

**Request Handling:**
- `ElevatorSystem._requests`: `ConcurrentQueue<Request>` (lock-free)
- `Request.RequestId`: Thread-safe generation with `Interlocked.Increment`
- `AddRequest`: Safe for concurrent calls

**Dispatch Process:**
- `_dispatchLock`: Protects `FindBestElevator` scoring during concurrent access
- Single dispatcher loop dequeues requests and assigns to elevators
- Multiple per-elevator tasks process movements independently

**Elevator Operations:**
- `Elevator.CurrentFloor`: Lock-protected property
- `Elevator.State`: Lock-protected property
- `Elevator._targetFloors`: `ConcurrentQueue<int>` (lock-free)
- Each elevator operates independently with its own locks

### Testing Strategy

**Test Structure:**
- `RequestTests.cs`: 13 tests (validation, direction, thread-safe IDs)
- `ElevatorSystemTests.cs`: 47 tests (initialization, dispatch, integration)
- `ElevatorTests.cs`: 12 tests (movement, doors, FIFO, boundaries)

**Helper Methods:**
- `WaitForSystemIdle()`: Polls until all elevators IDLE and no targets (used in integration tests)
- Fast test timings: `doorOpenMs: 5-10`, `floorTravelMs: 5-10` for quick test execution

**Critical Test Cases:**
- Concurrent request handling (100+ requests)
- Idle vs busy elevator selection
- End-to-end request → dispatch → pickup → destination
- Downward rides (direction calculation)
- Edge cases (request at current floor, all elevators busy)

## File Organization

### New Files (Multi-Elevator)
- `src/ElevatorSystem/Request.cs` - Ride request model
- `src/ElevatorSystem/ElevatorSystem.cs` - Multi-elevator coordinator
- `tests/ElevatorSystem.Tests/RequestTests.cs` - Request unit tests
- `tests/ElevatorSystem.Tests/ElevatorSystemTests.cs` - System tests
- `docs/plan_multi_elevator_system.md` - Implementation plan

### Modified Files
- `src/ElevatorSystem/Program.cs` - Updated to use ElevatorSystem with new console interface

### Unchanged Files (Reused As-Is)
- `src/ElevatorSystem/Elevator.cs` - Core elevator logic (perfect for multi-elevator)
- `src/ElevatorSystem/ElevatorState.cs` - State enum
- `src/ElevatorSystem/Direction.cs` - Direction enum (now actively used)
- `tests/ElevatorSystem.Tests/ElevatorTests.cs` - Elevator unit tests (all still valid)

### Removed Files
- `src/ElevatorSystem/ElevatorController.cs` - Superseded by ElevatorSystem
- `tests/ElevatorSystem.Tests/ElevatorControllerTests.cs` - No longer needed

## Design Rationale

### Why Centralized Dispatcher?
- **Simpler**: Single decision point for request assignment
- **Testable**: Easy to unit test dispatch algorithm in isolation
- **No contention**: Elevators don't compete for requests
- **Maintainable**: All dispatch logic in one place

### Why Keep Elevator.cs Unchanged?
- **Proven**: Already thread-safe with 20+ concurrent tests passing
- **Independent**: Each elevator instance operates independently
- **Reusable**: `ConcurrentQueue<int>` works perfectly for both pickup and destination floors
- **Simplicity**: No need to modify working code

### Why Request Class?
- **Rich context**: Enables intelligent dispatch (pickup location, destination, direction)
- **Direction-aware**: Supports future same-direction optimization
- **Trackable**: RequestId for logging and debugging
- **Extensible**: Easy to add priority, passenger count, wait time, etc.

## Common Development Tasks

### Adding a New Elevator (if count < 5)
1. Change `ELEVATOR_COUNT` in `Program.cs`
2. Build and run - no code changes needed

### Modifying Dispatch Algorithm
1. Update `FindBestElevator()` in `ElevatorSystem.cs`
2. Add corresponding tests in `ElevatorSystemTests.cs`
3. Run full test suite to verify

### Request Priority (Already Implemented)
The system includes a simplified two-level priority system:
- **Normal**: Standard requests (default)
- **High**: VIP/urgent requests (processed first, ignores idle preference)

See `PRIORITY_SIMPLIFIED.md` for details.

### Changing Floor Range
1. Update `MIN_FLOOR` and `MAX_FLOOR` in `Program.cs`
2. Update initial floor distribution logic if needed
3. Run tests to verify

## Performance Characteristics

- **Request Assignment**: <1ms per request (O(n) dispatch, n = elevator count)
- **Concurrent Requests**: Handles 100+ concurrent requests safely
- **Test Suite**: 70 tests complete in ~6 seconds
- **Memory**: Minimal overhead (ConcurrentQueue + List storage)
- **Throughput**: 3 elevators can handle ~1 request/second sustained load
