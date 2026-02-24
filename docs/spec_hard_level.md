Hard Level: Advanced Enterprise System (Optional Bonus)
Additional Requirements:
• Implement different elevator types (express, local, freight)
• Add maintenance mode and emergency stops
• Implement advanced algorithms (SCAN, LOOK, or custom optimization)
• Add floor restrictions and VIP access
• Performance monitoring and analytics
Technical Specifications
Elevator States
enum ElevatorState {
IDLE, MOVING_UP, MOVING_DOWN, DOOR_OPENING, DOOR_OPEN, DOOR_CLOSING, MAINTENANCE
}
Direction Enum
enum Direction {
UP, DOWN
}
Implementation Guidelines
Thread Safety Considerations
• Use appropriate locks/mutexes for shared resources
• Ensure atomic operations for elevator state changes
• Handle race conditions in request assignment
• Consider using thread-safe collections
Performance Requirements
• System should handle 100+ concurrent requests efficiently
Response time for elevator assignment should be < 100ms
• Memory usage should remain reasonable under load
Error Handling
• Handle invalid floor requests gracefully
• Implement timeouts for stuck elevators
• Add proper exception handling for concurrent operations