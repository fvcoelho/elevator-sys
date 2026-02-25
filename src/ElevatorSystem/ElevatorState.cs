namespace ElevatorSystem;

public enum ElevatorState
{
    IDLE, //DOOR IS CLOSED
    MOVING_UP, //DOOR IS CLOSED
    MOVING_DOWN, //DOOR IS CLOSED
    DOOR_OPENING,
    DOOR_OPEN,
    DOOR_CLOSING,
    MAINTENANCE,
    EMERGENCY_STOP //DOOR_OPEN
}
