# Plan: Multi-Elevator System Implementation (Medium Level)

## Overview
Upgrade the current single elevator system to support **3-5 elevators** (configurable) serving **floors 1-20** with **intelligent automatic dispatch**, load balancing, and same-direction priority.

## Architecture: Centralized Dispatcher Pattern

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

**Key Design Decisions:**
- **Reuse Elevator.cs**: No changes needed (perfect as-is)
- **Central dispatcher**: Single thread assigns requests, multiple elevator processors
- **Thread-safe**: ConcurrentQueue for requests, locks for state consistency
- **Automatic dispatch**: Smart algorithm (closest + same-direction + load balancing)
- **Simple architecture**: No DI, no frameworks, plain C# classes

## Configuration Constants

```csharp
const int ELEVATOR_COUNT = 3;  // Configurable: 3-5 elevators
const int MIN_FLOOR = 1;
const int MAX_FLOOR = 20;      // Extended from 10 to 20
const int DOOR_OPEN_MS = 3000;
const int FLOOR_TRAVEL_MS = 1500;
```

## New Components

### 1. Request Class
**File**: `src/ElevatorSystem/Request.cs` (NEW)

```csharp
public class Request
{
    public int PickupFloor { get; }
    public int DestinationFloor { get; }
    public Direction Direction { get; }      // Auto-calculated UP/DOWN
    public long Timestamp { get; }
    public int RequestId { get; }            // For tracking

    // Constructor validates floors, calculates direction
    // Thread-safe ID generation with Interlocked.Increment
}
```

**Purpose**: Captures complete ride request with pickup, destination, direction, and timestamp for intelligent dispatch.

### 2. ElevatorSystem Class
**File**: `src/ElevatorSystem/ElevatorSystem.cs` (NEW)

**Key Responsibilities:**
- Manage List<Elevator> (3-5 instances)
- ConcurrentQueue<Request> for incoming requests
- Intelligent dispatch algorithm (FindBestElevator)
- Start/manage processing tasks for all elevators
- System-wide status reporting

**Core Methods:**

```csharp
// Public API
public void AddRequest(Request request)
public async Task ProcessRequestsAsync(CancellationToken ct)
public string GetSystemStatus()

// Internal Logic
private int? FindBestElevator(Request)       // Returns elevator index or null
private int CalculateDistance(Elevator, int floor)  // |currentFloor - floor|
private void AssignRequestToElevator(int index, Request)
private async Task ProcessElevatorAsync(int index, CancellationToken)
```

**How Rides Work (Between Floors):**

Each ride request specifies **pickup floor** and **destination floor**:
- User at floor 3 wants to go to floor 5 → Request(pickup=3, dest=5, dir=UP)
- User at floor 5 wants to go to floor 3 → Request(pickup=5, dest=3, dir=DOWN)

**Assignment Process:**
```csharp
AssignRequestToElevator(elevator, request):
  1. Add request.PickupFloor to elevator's queue    // Elevator goes here first
  2. Add request.DestinationFloor to elevator's queue // Then goes here

  Example: Request floor 3 → 5
    - Elevator queue: [..., 3, 5]
    - Elevator moves to floor 3 → opens doors (pickup passenger)
    - Elevator moves to floor 5 → opens doors (drop off passenger)
```

**Edge Cases Handled:**
- Elevator already at pickup floor: Just opens doors (no movement)
- Request same floor as previous destination: Duplicate prevention works
- Downward rides (e.g., 10 → 2): Direction calculated as DOWN, works correctly

**Dispatch Algorithm - Simple + Idle Preference:**
```
1. Filter elevators into two groups:
   - IDLE elevators (state == IDLE)
   - BUSY elevators (state != IDLE)

2. If any IDLE elevators exist:
   - Pick closest idle elevator to pickup floor

3. Otherwise (all elevators busy):
   - Pick closest busy elevator to pickup floor

Distance = |elevator.CurrentFloor - request.PickupFloor|
```

**Example:**
- Request: Floor 10 (pickup)
- Elevator A: Floor 12, IDLE → Distance: 2 ✓ Best (idle and closest)
- Elevator B: Floor 8, MOVING_UP → Distance: 2 (busy, not chosen)
- Elevator C: Floor 15, IDLE → Distance: 5 (idle but farther)

**Another Example (all busy):**
- Request: Floor 10 (pickup)
- Elevator A: Floor 12, MOVING_UP → Distance: 2 ✓ Best (closest)
- Elevator B: Floor 8, MOVING_DOWN → Distance: 2 (tie, either works)
- Elevator C: Floor 15, MOVING_UP → Distance: 5

**Thread Coordination:**
- Main dispatcher loop: Dequeue requests, score elevators, assign
- Per-elevator tasks: Process assigned targets independently
- _dispatchLock: Protects scoring during concurrent access
- Each Elevator: Own ConcurrentQueue and locks (from current design)

**Initial Elevator Distribution:**
Elevators start at evenly distributed floors for better coverage:
- 3 elevators: Floors 1, 10, 20
- 4 elevators: Floors 1, 7, 14, 20
- 5 elevators: Floors 1, 5, 10, 15, 20

## Modified Components

### 3. Program.cs
**File**: `src/ElevatorSystem/Program.cs` (MODIFY)

**Changes:**
- Replace single `ElevatorController` with `ElevatorSystem`
- Update configuration constants (20 floors, configurable elevator count)
- New console interface for pickup + destination input
- Display all elevator statuses in real-time
- Keep instant quit with 'Q' key

**New Interface Flow:**
```
=== ELEVATOR SYSTEM (3 elevators, floors 1-20) ===

Press [R] to REQUEST a ride
Press [S] to view STATUS
Press [Q] to QUIT

Current Status:
Elevator 0: Floor 5  | IDLE      | Targets: []
Elevator 1: Floor 10 | MOVING_UP | Targets: [15, 18]
Elevator 2: Floor 15 | IDLE      | Targets: []

Pending Requests: 1

[User presses R]
> Pickup floor (1-20): 3
> Destination floor (1-20): 17

[SYSTEM] Request #5: Floor 3 → 17 (UP) added to queue
[DISPATCH] Request #5 → Elevator 0 (at floor 5, IDLE)
```

### 4. Direction Enum
**File**: `src/ElevatorSystem/Direction.cs` (ALREADY EXISTS - NOW USED)

Currently defined but unused. Now actively used in:
- Request.Direction (auto-calculated)
- Dispatch algorithm (same-direction priority)

## Unchanged Components

### 5. Elevator Class
**File**: `src/ElevatorSystem/Elevator.cs` (NO CHANGES)

**Why it's perfect:**
- Thread-safe with ConcurrentQueue<int> for targets
- Lock-based state management works for multi-elevator
- AddRequest(floor) handles both pickup and destination
- Each elevator instance is independent
- Proven thread-safety with 20+ concurrent requests

### 6. ElevatorState Enum
**File**: `src/ElevatorSystem/ElevatorState.cs` (NO CHANGES)

Already perfect for multi-elevator:
- IDLE, MOVING_UP, MOVING_DOWN, DOOR_OPEN
- Comments clarify door states
- Used in dispatch scoring

## Testing Strategy

### New Test Files

**1. RequestTests.cs** (NEW)
- Constructor validation (valid/invalid floors)
- Direction calculation (UP/DOWN)
- Thread-safe ID generation
- ToString formatting

**2. ElevatorSystemTests.cs** (NEW)

**Test Categories:**

A. **Initialization Tests**
```csharp
- Constructor with valid elevator count (1-5)
- Constructor with invalid count (0, 6+) throws exception
- Elevators distributed across floors
- All elevators start at IDLE state
```

B. **Request Management Tests**
```csharp
- AddRequest with valid floors
- AddRequest with invalid floors throws exception
- Request enqueued in ConcurrentQueue
- Multiple concurrent AddRequest calls (thread-safe)
```

C. **Dispatch Algorithm Tests** (Critical)
```csharp
- Closest idle elevator selected (multiple IDLE elevators)
- Idle preferred over closer busy elevator
- When all busy, pick closest busy elevator
- Tie-breaking: equal distance, pick consistently (first or deterministic)
- Edge case: All elevators at same distance
```

D. **Multi-Elevator Concurrent Tests**
```csharp
- 100+ concurrent requests processed correctly
- All elevators process simultaneously
- No requests lost or duplicated
- System reaches IDLE state after load completes
```

E. **Integration Tests**
```csharp
- End-to-end: Request → Dispatch → Pickup → Destination
  - Example: Request floor 3 → 17, verify elevator stops at both
  - Example: Request floor 15 → 5 (downward), verify correct direction
- Multiple passengers with different destinations
  - Request 3 → 10 and 5 → 8, verify both complete correctly
- FIFO processing per elevator
- System status accuracy during operation
- Edge case: Request from current floor (e.g., elevator at 5, request 5 → 10)
```

**Test Utilities:**
```csharp
private ElevatorSystem CreateTestSystem(int elevatorCount = 3)
{
    return new ElevatorSystem(elevatorCount, 1, 20,
                             doorOpenMs: 5,      // Fast for tests
                             floorTravelMs: 10); // Fast for tests
}

private async Task WaitForSystemIdle(ElevatorSystem system,
                                     TimeSpan timeout)
{
    // Poll until all elevators IDLE and no targets
    // Throw TimeoutException if not reached
}
```

### Existing Test Files (MINIMAL UPDATES)

**3. ElevatorTests.cs** (Keep as-is)
- All 12 tests remain valid (Elevator unchanged)
- Tests single elevator behavior independently

**4. ElevatorControllerTests.cs** (Optional: Remove or Archive)
- No longer relevant (replaced by ElevatorSystem)
- Can be archived for reference
- Or delete if confident in new ElevatorSystemTests coverage

## Implementation Steps

### Phase 0: Save Plan (First Step)

**Step 0.1: Copy Plan to Repository**
- Copy this plan file to: `docs/plan_multi_elevator_system.md`
- This makes the plan part of the repository for reference
- Can be committed alongside the implementation

### Phase 1: Core Foundation (Days 1-2)

**Step 1.1: Create Request Class**
- File: `src/ElevatorSystem/Request.cs`
- Implement immutable properties
- Direction auto-calculation
- Thread-safe ID generation
- Validation in constructor

**Step 1.2: Create RequestTests**
- File: `tests/ElevatorSystem.Tests/RequestTests.cs`
- Test all Request functionality
- Verify thread-safe ID generation
- Run tests: Should see 8-10 new passing tests

### Phase 2: ElevatorSystem Skeleton (Days 3-4)

**Step 2.1: Create ElevatorSystem Class (Basic)**
- File: `src/ElevatorSystem/ElevatorSystem.cs`
- Constructor with elevator initialization
- AddRequest method
- GetSystemStatus method
- No dispatch logic yet (just skeleton)

**Step 2.2: Basic ElevatorSystemTests**
- Test initialization
- Test AddRequest validation
- Test GetSystemStatus format
- Run tests: Should see 5-8 new passing tests

### Phase 3: Dispatch Algorithm (Days 5-6)

**Step 3.1: Implement FindBestElevator**
- Filter elevators by IDLE vs BUSY state
- Calculate distance for each: |currentFloor - pickupFloor|
- Prefer IDLE elevators
- Pick closest within preferred group
- Handle ties (pick first or random)

**Step 3.2: Dispatch Algorithm Tests**
- Test scoring scenarios
- Mock different elevator states
- Verify correct selection
- Run tests: Should see 10-15 new passing tests

**Step 3.3: Implement AssignRequestToElevator**
- Add request.PickupFloor to elevator queue (passenger pickup)
- Add request.DestinationFloor to elevator queue (passenger dropoff)
- Both floors added sequentially (FIFO order preserved)
- Logging: Show which elevator assigned, current position
- Handles edge case: elevator already at pickup floor

### Phase 4: Processing Loops (Days 7-8)

**Step 4.1: Implement ProcessRequestsAsync**
- Main dispatcher loop
- Request dequeuing
- Call FindBestElevator
- Call AssignRequestToElevator
- CancellationToken handling

**Step 4.2: Implement ProcessElevatorAsync**
- Per-elevator processing task
- Reuse existing Elevator movement logic
- Handle IDLE → process targets → IDLE cycle
- Start all elevator tasks in ProcessRequestsAsync

**Step 4.3: Integration Tests**
- End-to-end request processing
- Multi-elevator concurrent scenarios
- System idle detection
- Run full test suite: Target 40-50 passing tests total

### Phase 5: Console Interface (Day 9)

**Step 5.1: Update Program.cs**
- Replace ElevatorController with ElevatorSystem
- Update constants (ELEVATOR_COUNT, MAX_FLOOR=20)
- New request input flow (pickup → destination)
- System status display for all elevators

**Step 5.2: Manual Testing**
- Request multiple rides
- Observe dispatch decisions
- Verify elevator movements
- Test edge cases (all elevators busy)

### Phase 6: Polish & Verification (Day 10)

**Step 6.1: Code Review**
- Thread safety audit
- No race conditions
- Proper lock usage
- CancellationToken handling

**Step 6.2: Documentation**
- Update README.md with multi-elevator usage
- Update CLAUDE.md with new architecture
- Add inline comments where needed

**Step 6.3: Final Testing**
- Run full test suite (target: 100% pass)
- Stress test: 200+ concurrent requests
- Performance validation
- Memory leak check (long-running test)

## Critical Files

### Files to Create
1. **`src/ElevatorSystem/Request.cs`** - Request model (pickup, destination, direction)
2. **`src/ElevatorSystem/ElevatorSystem.cs`** - Multi-elevator coordinator and dispatcher
3. **`tests/ElevatorSystem.Tests/RequestTests.cs`** - Request unit tests
4. **`tests/ElevatorSystem.Tests/ElevatorSystemTests.cs`** - Dispatch & multi-elevator tests

### Files to Modify
5. **`src/ElevatorSystem/Program.cs`** - Update to use ElevatorSystem, new console interface

### Files to Reference (No Changes)
6. **`src/ElevatorSystem/Elevator.cs`** - Core elevator (perfect as-is)
7. **`src/ElevatorSystem/ElevatorState.cs`** - State enum (perfect as-is)
8. **`src/ElevatorSystem/Direction.cs`** - Direction enum (now actively used)

### Files to Keep
9. **`tests/ElevatorSystem.Tests/ElevatorTests.cs`** - Keep all tests (12 tests)

### Files to Consider Removing
10. **`src/ElevatorSystem/ElevatorController.cs`** - No longer needed (superseded by ElevatorSystem)
11. **`tests/ElevatorSystem.Tests/ElevatorControllerTests.cs`** - No longer needed

## Verification Checklist

### Build & Test
- [ ] `dotnet build` succeeds with 0 warnings
- [ ] `dotnet test` shows 40-50+ tests passing
- [ ] No test failures or flakiness
- [ ] Code coverage >85% on new components

### Functional Testing
- [ ] Start system with 3 elevators (configurable constant)
- [ ] Request ride from floor 1 → 20
- [ ] Observe elevator assignment and movement
- [ ] Request multiple concurrent rides
- [ ] Verify load balancing (requests distributed)
- [ ] Test same-direction priority (request "on the way")
- [ ] All elevators return to IDLE after requests complete
- [ ] System handles 50+ rapid requests correctly

### Thread Safety
- [ ] No race conditions during concurrent requests
- [ ] No deadlocks or hangs
- [ ] Proper lock usage (minimal contention)
- [ ] CancellationToken triggers clean shutdown
- [ ] All tasks complete gracefully

### Performance
- [ ] Request assignment <1ms per request
- [ ] System handles 100+ concurrent requests
- [ ] No memory leaks (observe long-running test)
- [ ] CPU usage reasonable during idle

## Expected Outcomes

### Test Results
- **Before**: 20 tests passing (single elevator)
- **After**: 40-50+ tests passing (multi-elevator)
- **Coverage**: >85% on ElevatorSystem and Request classes

### Console Output Example
```
=== ELEVATOR SYSTEM (3 elevators, floors 1-20) ===

Elevator 0: Floor 1  | IDLE      | Targets: []
Elevator 1: Floor 10 | MOVING_UP | Targets: [15, 18]
Elevator 2: Floor 20 | IDLE      | Targets: []

Pending Requests: 0

Press [R] to REQUEST a ride
Press [S] to view STATUS
Press [Q] to QUIT

> R
Pickup floor (1-20): 5
Destination floor (1-20): 18

[SYSTEM] Request #1: Floor 5 → 18 (UP) added to queue
[DISPATCH] Request #1 → Elevator 1 (at floor 10, MOVING_UP)
Added floor 5 → Queue: [15, 18, 5]
Added floor 18 → Queue: [15, 18, 5, 18]

Elevator 1 moving up to floor 15...
Doors are OPEN at floor 15
Doors are CLOSED (IDLE) at floor 15
[ELEVATOR 1] Arrived at floor 15
...
```

### Key Features Delivered
✅ 3-5 configurable elevators (change ELEVATOR_COUNT constant)
✅ Floors 1-20 support (extended from 1-10)
✅ Intelligent automatic dispatch (closest + same-direction + load balance)
✅ Thread-safe concurrent request handling
✅ Load balancing across elevators
✅ Request prioritization (FIFO with aging potential)
✅ Comprehensive logging and status reporting
✅ Enhanced test coverage for multi-elevator scenarios
✅ Maintains simple architecture (no DI, no frameworks)
✅ Reuses existing Elevator.cs (no modifications)

## Design Rationale

### Why Centralized Dispatcher?
- **Simpler**: Single decision point for request assignment
- **Testable**: Easy to unit test dispatch algorithm
- **No contention**: Elevators don't compete for requests
- **Maintainable**: All logic in one place

### Why Keep Elevator.cs Unchanged?
- **Proven**: Already thread-safe with 20+ concurrent tests passing
- **Independent**: Each elevator operates independently
- **Reusable**: ConcurrentQueue<int> works for both pickup and destination

### Why Request Class?
- **Rich context**: Enables intelligent dispatch decisions
- **Direction-aware**: Supports same-direction priority
- **Trackable**: RequestId for logging and debugging
- **Extensible**: Easy to add priority, passenger count, etc.

### Why Scoring Algorithm?
- **Configurable**: Easy to adjust weights (-50, +3, etc.)
- **Understandable**: Clear numeric scoring (not black-box)
- **Extensible**: Can add more factors (energy, maintenance, etc.)
- **Testable**: Deterministic outcomes for given inputs

## Estimated Effort

- **Phase 1**: Core Foundation (Days 1-2) - Request class + tests
- **Phase 2**: ElevatorSystem Skeleton (Days 3-4) - Basic structure
- **Phase 3**: Dispatch Algorithm (Days 5-6) - Scoring logic
- **Phase 4**: Processing Loops (Days 7-8) - Async coordination
- **Phase 5**: Console Interface (Day 9) - User interaction
- **Phase 6**: Polish & Verification (Day 10) - Testing & docs

**Total: 10 days (can be compressed to 5-7 days with full-time focus)**

## Risk Mitigation

### Risk 1: Race Conditions in Dispatch
**Mitigation**: _dispatchLock protects scoring, ConcurrentQueue for requests

### Risk 2: Request Starvation
**Mitigation**: FIFO queue ensures oldest request processed first

### Risk 3: Test Flakiness
**Mitigation**: Short delays (5-10ms), WaitForSystemIdle helper, avoid absolute timing

### Risk 4: Complexity Creep
**Mitigation**: Follow phased approach, test after each phase, keep architecture simple

## Future Enhancements (Post-MVP)

- SCAN/LOOK algorithm (sweep motion optimization)
- Request cancellation
- Elevator maintenance mode
- Statistics dashboard (avg wait time, utilization)
- Energy optimization (predictive parking)
- Zone assignments for peak hours
