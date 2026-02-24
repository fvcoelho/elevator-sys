using System.Collections.Concurrent;

namespace ElevatorSystem;

public class ElevatorSystem
{
    private readonly List<Elevator> _elevators;
    private readonly ConcurrentQueue<Request> _requests = new();
    private readonly object _dispatchLock = new();
    private readonly int _minFloor;
    private readonly int _maxFloor;
    private readonly List<Task> _elevatorTasks = new();

    public int ElevatorCount => _elevators.Count;
    public int PendingRequestCount => _requests.Count;

    public ElevatorSystem(int elevatorCount, int minFloor, int maxFloor, int doorOpenMs = 3000, int floorTravelMs = 1500)
    {
        if (elevatorCount < 1 || elevatorCount > 5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elevatorCount),
                "Elevator count must be between 1 and 5");
        }

        if (minFloor >= maxFloor)
        {
            throw new ArgumentException("Min floor must be less than max floor");
        }

        _minFloor = minFloor;
        _maxFloor = maxFloor;
        _elevators = new List<Elevator>(elevatorCount);

        // Initialize elevators at evenly distributed floors for better coverage
        var initialFloors = CalculateInitialFloors(elevatorCount, minFloor, maxFloor);

        for (int i = 0; i < elevatorCount; i++)
        {
            var elevator = new Elevator(
                minFloor: minFloor,
                maxFloor: maxFloor,
                initialFloor: initialFloors[i],
                doorOpenMs: doorOpenMs,
                floorTravelMs: floorTravelMs,
                label: GetElevatorLabel(i));

            _elevators.Add(elevator);
        }
    }

    private static int[] CalculateInitialFloors(int elevatorCount, int minFloor, int maxFloor)
    {
        var floors = new int[elevatorCount];

        if (elevatorCount == 1)
        {
            floors[0] = minFloor;
        }
        else
        {
            // Distribute elevators evenly across the floor range
            // For 3 elevators on floors 1-20: want 1, 10, 20 (not 1, 11, 20)
            // For 5 elevators on floors 1-20: want 1, 5, 10, 15, 20 (not 1, 6, 11, 16, 20)
            var floorRange = maxFloor - minFloor;

            for (int i = 0; i < elevatorCount; i++)
            {
                if (i == 0)
                {
                    floors[i] = minFloor;
                }
                else if (i == elevatorCount - 1)
                {
                    floors[i] = maxFloor;
                }
                else
                {
                    // For middle elevators, distribute evenly
                    var position = (double)i / (elevatorCount - 1);
                    floors[i] = minFloor + (int)(floorRange * position);
                }
            }
        }

        return floors;
    }

    public void AddRequest(Request request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Validate floors are within system range
        if (request.PickupFloor < _minFloor || request.PickupFloor > _maxFloor)
        {
            throw new ArgumentException(
                $"Pickup floor {request.PickupFloor} is outside valid range ({_minFloor}-{_maxFloor})");
        }

        if (request.DestinationFloor < _minFloor || request.DestinationFloor > _maxFloor)
        {
            throw new ArgumentException(
                $"Destination floor {request.DestinationFloor} is outside valid range ({_minFloor}-{_maxFloor})");
        }

        _requests.Enqueue(request);
        Console.WriteLine($"[SYSTEM] {request} added to queue");
    }

    public string GetSystemStatus()
    {
        var status = new System.Text.StringBuilder();
        status.AppendLine($"=== ELEVATOR SYSTEM ({ElevatorCount} elevators, floors {_minFloor}-{_maxFloor}) ===");
        status.AppendLine();

        for (int i = 0; i < _elevators.Count; i++)
        {
            var elevator = _elevators[i];
            var targets = elevator.GetTargets();
            var targetStr = targets.Any() ? $"[{string.Join(", ", targets)}]" : "[]";

            status.AppendLine($"Elevator {GetElevatorLabel(i)}: Floor {elevator.CurrentFloor,-2} | {elevator.State,-12} | Targets: {targetStr}");
        }

        status.AppendLine();
        status.AppendLine($"Pending Requests: {PendingRequestCount}");

        return status.ToString();
    }

    public Elevator GetElevator(int index)
    {
        if (index < 0 || index >= _elevators.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _elevators[index];
    }

    public int? FindBestElevator(Request request)
    {
        lock (_dispatchLock)
        {
            // Separate elevators into idle and busy groups
            var idleElevators = new List<(int index, int distance)>();
            var busyElevators = new List<(int index, int distance)>();

            for (int i = 0; i < _elevators.Count; i++)
            {
                var elevator = _elevators[i];
                var distance = CalculateDistance(elevator.CurrentFloor, request.PickupFloor);

                if (elevator.State == ElevatorState.IDLE)
                {
                    idleElevators.Add((i, distance));
                }
                else
                {
                    busyElevators.Add((i, distance));
                }
            }

            // Prefer idle elevators
            if (idleElevators.Any())
            {
                // Find closest idle elevator
                var best = idleElevators.OrderBy(e => e.distance).First();
                return best.index;
            }

            // All elevators are busy - pick closest one
            if (busyElevators.Any())
            {
                var best = busyElevators.OrderBy(e => e.distance).First();
                return best.index;
            }

            // No elevators available (shouldn't happen)
            return null;
        }
    }

    private int CalculateDistance(int currentFloor, int targetFloor)
    {
        return Math.Abs(currentFloor - targetFloor);
    }

    private static string GetElevatorLabel(int index)
    {
        // Convert 0->A, 1->B, 2->C, 3->D, 4->E
        return ((char)('A' + index)).ToString();
    }

    public void AssignRequestToElevator(int elevatorIndex, Request request)
    {
        if (elevatorIndex < 0 || elevatorIndex >= _elevators.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(elevatorIndex));
        }

        var elevator = _elevators[elevatorIndex];

        // Add pickup floor first, then destination floor
        // This ensures the elevator goes to pickup the passenger before going to their destination
        elevator.AddRequest(request.PickupFloor);
        elevator.AddRequest(request.DestinationFloor);

        Console.WriteLine($"[DISPATCH] {request} → Elevator {GetElevatorLabel(elevatorIndex)} (at floor {elevator.CurrentFloor}, {elevator.State})");
    }

    public async Task ProcessRequestsAsync(CancellationToken cancellationToken = default)
    {
        // Start all elevator processing tasks
        for (int i = 0; i < _elevators.Count; i++)
        {
            var index = i;
            var task = ProcessElevatorAsync(index, cancellationToken);
            _elevatorTasks.Add(task);
        }

        // Main dispatcher loop - process incoming requests
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_requests.TryDequeue(out var request))
            {
                // Find best elevator and assign the request
                var bestElevatorIndex = FindBestElevator(request);

                if (bestElevatorIndex.HasValue)
                {
                    AssignRequestToElevator(bestElevatorIndex.Value, request);
                }
                else
                {
                    // No elevator available - re-enqueue the request
                    _requests.Enqueue(request);
                    Console.WriteLine($"[SYSTEM] No elevator available, re-queueing {request}");
                }
            }

            // Small delay to prevent tight loop
            await Task.Delay(50, cancellationToken);
        }

        // Wait for all elevator tasks to complete
        await Task.WhenAll(_elevatorTasks);
    }

    private async Task ProcessElevatorAsync(int elevatorIndex, CancellationToken cancellationToken)
    {
        var elevator = _elevators[elevatorIndex];

        while (!cancellationToken.IsCancellationRequested)
        {
            if (elevator.TryGetNextTarget(out int targetFloor))
            {
                // Move to target floor
                while (elevator.CurrentFloor != targetFloor && !cancellationToken.IsCancellationRequested)
                {
                    if (elevator.CurrentFloor < targetFloor)
                    {
                        await elevator.MoveUp();
                    }
                    else if (elevator.CurrentFloor > targetFloor)
                    {
                        await elevator.MoveDown();
                    }
                }

                // Open and close doors at target floor
                if (!cancellationToken.IsCancellationRequested)
                {
                    await elevator.OpenDoor();
                    await elevator.CloseDoor();
                    Console.WriteLine($"[ELEVATOR {GetElevatorLabel(elevatorIndex)}] Arrived at floor {targetFloor}");
                }
            }
            else
            {
                // No targets, wait a bit
                await Task.Delay(100, cancellationToken);
            }
        }
    }
}
