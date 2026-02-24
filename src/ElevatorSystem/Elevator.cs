namespace ElevatorSystem;

public class Elevator
{
    private readonly object _lock = new();
    private int _currentFloor;
    private ElevatorState _state;

    public int MinFloor { get; }
    public int MaxFloor { get; }
    public int DoorOpenMs { get; }
    public int FloorTravelMs { get; }
    public string Label { get; }

    public int CurrentFloor
    {
        get
        {
            lock (_lock)
            {
                return _currentFloor;
            }
        }
        private set
        {
            lock (_lock)
            {
                _currentFloor = value;
            }
        }
    }

    public ElevatorState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
        private set
        {
            lock (_lock)
            {
                _state = value;
            }
        }
    }

    public Elevator(int minFloor, int maxFloor, int initialFloor, int doorOpenMs, int floorTravelMs, string label = "")
    {
        if (initialFloor < minFloor || initialFloor > maxFloor)
        {
            throw new ArgumentException($"Initial floor must be between {minFloor} and {maxFloor}");
        }

        MinFloor = minFloor;
        MaxFloor = maxFloor;
        DoorOpenMs = doorOpenMs;
        FloorTravelMs = floorTravelMs;
        Label = label;
        _currentFloor = initialFloor;
        _state = ElevatorState.IDLE;
    }

    public async Task MoveUp()
    {
        lock (_lock)
        {
            if (_currentFloor >= MaxFloor)
            {
                throw new InvalidOperationException($"Cannot move up from floor {MaxFloor}");
            }
            _state = ElevatorState.MOVING_UP;
        }

        await Task.Delay(FloorTravelMs);

        lock (_lock)
        {
            _currentFloor++;
        }

        Console.WriteLine($"[ELEVATOR {Label}] moved up to floor {CurrentFloor}");
    }

    public async Task MoveDown()
    {
        lock (_lock)
        {
            if (_currentFloor <= MinFloor)
            {
                throw new InvalidOperationException($"Cannot move down from floor {MinFloor}");
            }
            _state = ElevatorState.MOVING_DOWN;
        }

        await Task.Delay(FloorTravelMs);

        lock (_lock)
        {
            _currentFloor--;
        }

        Console.WriteLine($"[ELEVATOR {Label}] moved down to floor {CurrentFloor}");
    }

    public async Task OpenDoor()
    {
        State = ElevatorState.DOOR_OPEN;
        Console.WriteLine($"[ELEVATOR {Label}] Doors are OPEN at floor {CurrentFloor}");
        await Task.Delay(DoorOpenMs);
    }

    public async Task CloseDoor()
    {
        Console.WriteLine($"[ELEVATOR {Label}] Doors are CLOSED (IDLE) at floor {CurrentFloor}");
        State = ElevatorState.IDLE;
        await Task.CompletedTask;
    }
}
