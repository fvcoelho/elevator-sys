Medium Level: Multiple Elevator System
Requirements:
• Design a system with 3-5 elevators serving floors 1-20
• Implement an intelligent dispatch algorithm to assign requests to elevators
• Handle concurrent requests from multiple passengers
• Ensure thread safety using appropriate synchronization mechanisms
• Implement elevator optimization (minimize wait time)
Additional Classes:
class ElevatorSystem:
- elevators: List<Elevator>
- requestQueue: Queue<Request>
- methods: assignRequest), findBestElevator), balanceLoad()
class Request:
- pickupFloor: int
- destinationFloor: int
- direction: Direction
- timestamp: long
Expected Features:
• Multi-threaded request processing
• Elevator assignment optimization (closest elevator, same direction priority)
• Load balancing between elevators
• Request prioritization
• Comprehensive logging and status reporting

