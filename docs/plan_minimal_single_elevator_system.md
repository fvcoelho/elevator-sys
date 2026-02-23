Plan: .NET 9 Minimal Single Elevator System

Prompt: "Create a cross-platform console application using .NET 9 to simulate a single elevator system. The application should be minimal, with a clear console interface for input (e.g., request pickup floors). Implement an Elevator class and an ElevatorController class, with basic operations like move up, move down, add requests. Ensure thread safety for concurrent requests. After implementing the system, create minimal unit tests for each class to validate core behaviors (e.g., moving floors, adding requests). Keep the implementation and tests focused on simplicity and correctness."

 Overview

 Create a minimal, straightforward .NET 9 console application simulating a
 single elevator serving floors 1-10 with FIFO scheduling and thread-safe
 operations.

 Architecture: Minimal & Simple

 - No dependency injection
 - No hosting/background services
 - Simple console application with manual threading
 - Focus on correctness and clarity

 Project Structure

 elevator-sys/
 ├── src/
 │   └── ElevatorSystem/s
 │       ├── ElevatorSystem.csproj
 │       ├── Program.cs
 │       ├── ElevatorState.cs (enum)
 │       ├── Direction.cs (enum)
 │       ├── Elevator.cs
 │       └── ElevatorController.cs
 ├── tests/
 │   └── ElevatorSystem.Tests/
 │       ├── ElevatorSystem.Tests.csproj
 │       ├── ElevatorTests.cs
 │       └── ElevatorControllerTests.cs
 └── ElevatorSystem.sln

 Implementation Details

 1. Configuration Constants (in Program.cs)

 const int MIN_FLOOR = 1;
 const int MAX_FLOOR = 10;
 const int INITIAL_FLOOR = 1;
 const int DOOR_OPEN_MS = 2000;      // 2 seconds
 const int FLOOR_TRAVEL_MS = 1500;   // 1.5 seconds per floor

 2. Enums

 ElevatorState.cs:
 public enum ElevatorState
 {
     IDLE,
     MOVING_UP,
     MOVING_DOWN,
     DOOR_OPEN
 }

 Direction.cs:
 public enum Direction
 {
     NONE,
     UP,
     DOWN
 }

 3. Elevator Class

 Key Features:
 - Properties: CurrentFloor (int), State (ElevatorState)
 - Thread-safe target floor queue using ConcurrentQueue<int>
 - Simple lock object for state/floor synchronization
 - Methods:
   - MoveUp() - async, increments floor, delays for travel time
   - MoveDown() - async, decrements floor, delays for travel time
   - OpenDoor() - async, sets state, delays for door open time
   - CloseDoor() - async, resets to IDLE state
   - AddRequest(int floor) - validates and enqueues floor
   - TryGetNextTarget(out int floor) - dequeues next floor
   - HasTargets() - checks if queue has items

 Thread Safety:
 - Use lock for CurrentFloor and State properties
 - Use ConcurrentQueue<int> for target floors (lock-free)

 4. ElevatorController Class

 Key Features:
 - Holds reference to single Elevator instance
 - Request queue using ConcurrentQueue<int> (just floors, FIFO)
 - Methods:
   - RequestElevator(int floor) - validates and enqueues request
   - ProcessRequestsAsync(CancellationToken) - main processing loop
   - GetStatus() - returns current floor, state, queue info

 Processing Loop Logic:
 1. Dequeue request from controller queue → add to elevator targets
 2. If elevator is IDLE and has targets:
    a. Get next target floor
    b. Move to target (loop: moveUp/moveDown until reached)
    c. Open doors
    d. Close doors
 3. Delay 100ms before next iteration

 Thread Safety:
 - ConcurrentQueue for requests
 - Single processing loop (only one thread calls ProcessRequestsAsync)

 5. Program.cs - Console Interface

 Structure:
 - Create Elevator and ElevatorController instances
 - Start processing loop in background Task
 - Main loop:
   - Display current status (floor, state, queues)
   - Show menu: [R]equest floor, [S]tatus, [Q]uit
   - Handle user input
   - Validate floor input (1-10)

 Simple Console Display:
 === ELEVATOR SYSTEM ===
 Current Floor: 5
 State: IDLE
 Target Queue: [7, 3, 9]
 Pending Requests: 2

 Commands: [R]equest | [S]tatus | [Q]uit
 >

 6. Unit Tests

 ElevatorTests.cs:
 - MoveUp_IncreasesFloor() - verify floor increments
 - MoveDown_DecreasesFloor() - verify floor decrements
 - MoveUp_AtTopFloor_ThrowsException() - boundary test
 - MoveDown_AtBottomFloor_ThrowsException() - boundary test
 - AddRequest_ValidFloor_EnqueuesTarget() - FIFO test
 - AddRequest_InvalidFloor_ThrowsException() - validation test
 - OpenDoor_SetsState() - state transition test
 - CloseDoor_ResetsToIdle() - state transition test
 - TryGetNextTarget_FIFO_Order() - verify queue order

 ElevatorControllerTests.cs:
 - RequestElevator_ValidFloor_Enqueues() - request handling
 - RequestElevator_InvalidFloor_ThrowsException() - validation
 - ProcessRequests_MovesToTarget() - integration test with short delays
 - ProcessRequests_FIFO_Order() - verify processing order
 - ConcurrentRequests_AllProcessed() - thread safety test (100 concurrent
 requests)

 Test Configuration:
 - Use xUnit framework
 - FluentAssertions for readable assertions
 - Short timing delays for fast test execution (10ms travel, 5ms doors)

 Implementation Steps

 Step 1: Solution Setup

 1. Create solution: dotnet new sln -n ElevatorSystem
 2. Create console project: dotnet new console -n ElevatorSystem -o
 src/ElevatorSystem
 3. Update to .NET 9, enable nullable
 4. Create test project: dotnet new xunit -n ElevatorSystem.Tests -o
 tests/ElevatorSystem.Tests
 5. Add test dependencies: xUnit, FluentAssertions
 6. Add project references to solution

 Step 2: Core Implementation

 1. Create ElevatorState.cs enum
 2. Create Direction.cs enum
 3. Implement Elevator.cs:
   - Properties with lock-based thread safety
   - ConcurrentQueue for targets
   - Async movement methods with delays
   - Door operation methods
 4. Implement ElevatorController.cs:
   - ConcurrentQueue for requests
   - RequestElevator method with validation
   - ProcessRequestsAsync with main loop
   - GetStatus method

 Step 3: Console Interface

 1. Implement Program.cs:
   - Create instances
   - Start background processing task
   - Console input/output loop
   - Status display method
   - Request handling with validation
   - Graceful shutdown with CancellationToken

 Step 4: Unit Tests

 1. Implement ElevatorTests.cs:
   - Movement tests (up/down, boundaries)
   - State transition tests
   - Queue tests (add, dequeue, FIFO)
   - Validation tests
 2. Implement ElevatorControllerTests.cs:
   - Request handling tests
   - Processing loop tests (use short delays)
   - FIFO verification
   - Thread safety test (concurrent requests)
 3. Run all tests: dotnet test

 Step 5: Verification

 1. Build solution: dotnet build
 2. Run all tests: dotnet test - ensure 100% pass
 3. Manual testing:
   - Start application: dotnet run --project src/ElevatorSystem
   - Request multiple floors (test FIFO)
   - Request concurrent floors from different terminals if possible
   - Verify floor range validation (try 0, 11)
   - Test boundaries (floor 1 going down, floor 10 going up)
   - Check status display updates
 4. Test thread safety by rapid consecutive requests

 Critical Files

 Implementation:
 - /Users/fvcoelho/Working/elevator-sys/src/ElevatorSystem/Elevator.cs - Core
 elevator with thread-safe state and FIFO queue
 -
 /Users/fvcoelho/Working/elevator-sys/src/ElevatorSystem/ElevatorController.cs
 - Request processing and orchestration
 - /Users/fvcoelho/Working/elevator-sys/src/ElevatorSystem/Program.cs - Console
  interface and main loop

 Tests:
 - /Users/fvcoelho/Working/elevator-sys/tests/ElevatorSystem.Tests/ElevatorTest
 s.cs - Elevator behavior tests
 - /Users/fvcoelho/Working/elevator-sys/tests/ElevatorSystem.Tests/ElevatorCont
 rollerTests.cs - Controller and thread safety tests

 Design Decisions

 1. No DI/Hosting: Keep it simple, manual instance creation
 2. ConcurrentQueue: Lock-free FIFO, perfect for request queues
 3. Lock for State/Floor: Simple synchronization for shared state
 4. Single Processing Loop: One background task, easier to reason about
 5. Async/Await: Natural delays for simulation, non-blocking
 6. Direction Enum: Included per spec but only used for extensibility (not in
 FIFO logic)
 7. Simple Console UI: Focus on functionality, not fancy visuals

 Thread Safety Strategy

 1. Elevator.CurrentFloor: Lock-protected property
 2. Elevator.State: Lock-protected property
 3. Elevator._targetFloors: ConcurrentQueue (lock-free)
 4. Controller._requestQueue: ConcurrentQueue (lock-free)
 5. Processing Loop: Single-threaded (only one ProcessRequestsAsync call)
 6. User Input: Main thread, separate from processing

 Expected Behavior

 1. User requests floor via console
 2. Request added to controller queue (thread-safe)
 3. Processing loop dequeues request → adds to elevator targets
 4. Elevator processes targets FIFO:
   - Moves to floor (one floor at a time, showing movement)
   - Opens doors (2 second pause)
   - Closes doors
   - Returns to IDLE
 5. Status display updates showing current position

 Simplifications from Spec

 - Direction enum included but not used in FIFO logic (pure FIFO by floor only)
 - No request deduplication (all requests processed as-is)
 - No events (simpler without them)
 - No logging infrastructure (Console.WriteLine for key actions)
 - No configuration files (constants in code)

