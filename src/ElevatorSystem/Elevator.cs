using Microsoft.Extensions.Logging;

namespace ElevatorSystem;

public class Elevator
{
    private readonly object _lock = new();
    private int _currentFloor;
    private ElevatorState _state;
    private readonly ILogger? _logger;
    private bool _inMaintenance;
    private readonly object _maintenanceLock = new();

    public int MinFloor { get; }
    public int MaxFloor { get; }
    public int DoorOpenMs { get; }
    public int FloorTravelMs { get; }
    public int DoorTransitionMs { get; }
    public string Label { get; }
    public ElevatorType Type { get; }
    public HashSet<int> ServedFloors { get; }
    public int Capacity { get; }

    public bool InMaintenance
    {
        get { lock (_maintenanceLock) { return _inMaintenance; } }
    }

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

    public Elevator(
        int minFloor,
        int maxFloor,
        int initialFloor,
        int doorOpenMs,
        int floorTravelMs,
        string label = "",
        ILogger? logger = null,
        int doorTransitionMs = 1000,
        ElevatorType type = ElevatorType.Local,
        HashSet<int>? servedFloors = null,
        int capacity = 10)
    {
        if (initialFloor < minFloor || initialFloor > maxFloor)
        {
            throw new ArgumentException($"Initial floor must be between {minFloor} and {maxFloor}");
        }

        MinFloor = minFloor;
        MaxFloor = maxFloor;
        DoorOpenMs = doorOpenMs;
        FloorTravelMs = floorTravelMs;
        DoorTransitionMs = doorTransitionMs;
        Label = label;
        Type = type;
        ServedFloors = servedFloors ?? Enumerable.Range(minFloor, maxFloor - minFloor + 1).ToHashSet();
        Capacity = capacity;
        _logger = logger;
        _currentFloor = initialFloor;
        _state = ElevatorState.IDLE;
    }

    /// <summary>
    /// Check if this elevator can serve the specified floor
    /// </summary>
    public bool CanServeFloor(int floor)
    {
        return ServedFloors.Contains(floor);
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

        _logger?.LogInformation("State: MOVING_UP | Floor: {Floor}", CurrentFloor);
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

        _logger?.LogInformation("State: MOVING_DOWN | Floor: {Floor}", CurrentFloor);
    }

    public async Task OpenDoor()
    {
        State = ElevatorState.DOOR_OPENING;
        _logger?.LogInformation("State: DOOR_OPENING | Floor: {Floor}", CurrentFloor);
        await Task.Delay(DoorTransitionMs);

        State = ElevatorState.DOOR_OPEN;
        _logger?.LogInformation("State: DOOR_OPEN | Floor: {Floor}", CurrentFloor);
        await Task.Delay(DoorOpenMs);
    }

    public async Task CloseDoor()
    {
        State = ElevatorState.DOOR_CLOSING;
        _logger?.LogInformation("State: DOOR_CLOSING | Floor: {Floor}", CurrentFloor);
        await Task.Delay(DoorTransitionMs);

        State = ElevatorState.IDLE;
        _logger?.LogInformation("State: IDLE | Arrived at floor {Floor}", CurrentFloor);
    }

    public void EnterMaintenance()
    {
        lock (_maintenanceLock)
        {
            _inMaintenance = true;
            State = ElevatorState.MAINTENANCE;
            _logger?.LogWarning("Elevator {Label} entering MAINTENANCE mode", Label);
        }
    }

    public void ExitMaintenance()
    {
        lock (_maintenanceLock)
        {
            _inMaintenance = false;
            State = ElevatorState.IDLE;
            _logger?.LogInformation("Elevator {Label} exiting MAINTENANCE mode", Label);
        }
    }
}
