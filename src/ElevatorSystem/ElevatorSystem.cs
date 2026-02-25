using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ElevatorSystem;

// Track request progress through the system
internal class RequestProgress
{
    public int RequestId { get; set; }
    public int PickupFloor { get; set; }
    public int DestinationFloor { get; set; }
    public RequestPriority Priority { get; set; }
    public int AssignedElevatorIndex { get; set; }
    public bool PickupReached { get; set; }
    public bool DestinationReached { get; set; }
    public bool IsComplete => PickupReached && DestinationReached;
    public Queue<int> PendingFloors { get; set; } = new();
    public long RequestTimestamp { get; set; }
    public long? PickupTimestamp { get; set; }
    public long? DestinationTimestamp { get; set; }
}

public class ElevatorSystem
{
    // Dispatch algorithm scoring constants
    private const int SCAN_DIRECTION_BONUS = 100;
    private const int SCAN_IDLE_BONUS = 50;
    private const int LOOK_DIRECTION_BONUS = 75;
    private const int LOOK_IDLE_BONUS = 60;

    // System timing constants
    private const int REQUEST_LOOP_DELAY_MS = 50;
    private const int IDLE_DELAY_MS = 100;

    private readonly List<Elevator> _elevators;
    private readonly ConcurrentQueue<Request> _requests = new();
    private readonly object _dispatchLock = new();
    private readonly int _minFloor;
    private readonly int _maxFloor;
    private readonly List<Task> _elevatorTasks = new();

    // Single queue architecture - system-level target management
    private readonly Dictionary<int, Queue<int>> _elevatorTargets = new();
    private readonly object _targetLock = new();

    // Completion tracking
    private readonly ConcurrentDictionary<int, RequestProgress> _activeRequests = new();
    private readonly ConcurrentBag<int> _completedRequestIds = new();

    // Logging
    private readonly string _logsDirectory = "logs";
    private readonly Dictionary<int, StreamWriter> _elevatorLogs = new();
    private readonly object _logLock = new();

    // Floor access control
    private readonly Dictionary<int, FloorRestriction> _floorRestrictions = new();

    // Performance tracking
    private readonly PerformanceTracker _performanceTracker = new();

    public int ElevatorCount => _elevators.Count;
    public int PendingRequestCount => _requests.Count;
    public DispatchAlgorithm Algorithm { get; set; } = DispatchAlgorithm.Simple;

    public ElevatorSystem(int elevatorCount, int minFloor, int maxFloor, int doorOpenMs = 3000, int floorTravelMs = 1500, int doorTransitionMs = 1000)
        : this(minFloor, maxFloor, doorOpenMs, floorTravelMs, doorTransitionMs, CreateDefaultElevatorConfigs(elevatorCount, minFloor, maxFloor))
    {
    }

    public ElevatorSystem(
        int minFloor,
        int maxFloor,
        int doorOpenMs,
        int floorTravelMs,
        int doorTransitionMs,
        ElevatorConfig[] elevatorConfigs)
    {
        if (elevatorConfigs == null || elevatorConfigs.Length < 1 || elevatorConfigs.Length > 5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elevatorConfigs),
                "Elevator count must be between 1 and 5");
        }

        if (minFloor >= maxFloor)
        {
            throw new ArgumentException("Min floor must be less than max floor");
        }

        _minFloor = minFloor;
        _maxFloor = maxFloor;
        _elevators = new List<Elevator>(elevatorConfigs.Length);

        // Create logs directory if it doesn't exist
        if (!Directory.Exists(_logsDirectory))
        {
            Directory.CreateDirectory(_logsDirectory);
        }

        // Create elevators from configs
        for (int i = 0; i < elevatorConfigs.Length; i++)
        {
            var config = elevatorConfigs[i];
            var elevatorLabel = string.IsNullOrEmpty(config.Label) ? GetElevatorLabel(i) : config.Label;

            if (config.ServedFloors != null && !config.ServedFloors.Contains(config.InitialFloor))
            {
                throw new ArgumentException(
                    $"Elevator '{elevatorLabel}' has InitialFloor {config.InitialFloor} " +
                    $"which is not in its ServedFloors set");
            }

            // Initialize log file for this elevator
            var logFileName = $"elevator_{elevatorLabel}.log";
            var logFilePath = Path.Combine(_logsDirectory, logFileName);
            var logWriter = new StreamWriter(logFilePath, append: true) { AutoFlush = true };
            _elevatorLogs[i] = logWriter;

            // Create ILogger for this elevator
            ILogger elevatorLogger = new ElevatorFileLogger(logWriter, elevatorLabel);

            var elevator = new Elevator(
                minFloor: minFloor,
                maxFloor: maxFloor,
                initialFloor: config.InitialFloor,
                doorOpenMs: doorOpenMs,
                floorTravelMs: floorTravelMs,
                label: elevatorLabel,
                logger: elevatorLogger,
                doorTransitionMs: doorTransitionMs,
                type: config.Type,
                servedFloors: config.ServedFloors,
                capacity: config.Capacity);

            _elevators.Add(elevator);

            // Initialize target queue for this elevator
            _elevatorTargets[i] = new Queue<int>();

            // Initialize performance tracking for this elevator
            _performanceTracker.InitializeElevator(elevatorLabel);
        }
    }

    private static ElevatorConfig[] CreateDefaultElevatorConfigs(int elevatorCount, int minFloor, int maxFloor)
    {
        var initialFloors = CalculateInitialFloors(elevatorCount, minFloor, maxFloor);
        var configs = new ElevatorConfig[elevatorCount];

        for (int i = 0; i < elevatorCount; i++)
        {
            configs[i] = new ElevatorConfig
            {
                Label = GetElevatorLabel(i),
                InitialFloor = initialFloors[i],
                Type = ElevatorType.Local,
                ServedFloors = null, // All floors
                Capacity = 10
            };
        }

        return configs;
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

    /// <summary>
    /// Create a configuration with 1 Express elevator and 2 Local elevators
    /// Express serves lobby (minFloor) and top ~30% of floors, Local serve all floors
    /// </summary>
    public static ElevatorConfig[] CreateExpressLocalMix(int minFloor, int maxFloor)
    {
        var floorRange = maxFloor - minFloor + 1;
        if (floorRange < 4)
        {
            throw new ArgumentException(
                $"Express/Local mix requires at least 4 floors (got {floorRange}: {minFloor}-{maxFloor})");
        }

        var expressStartFloor = minFloor + (int)(floorRange * 0.7);
        if (maxFloor - expressStartFloor + 1 < 2)
            expressStartFloor = maxFloor - 1;

        var expressServedFloors = new HashSet<int> { minFloor };
        expressServedFloors.UnionWith(Enumerable.Range(expressStartFloor, maxFloor - expressStartFloor + 1));

        return new[]
        {
            new ElevatorConfig
            {
                Label = "A",
                InitialFloor = minFloor,
                Type = ElevatorType.Express,
                ServedFloors = expressServedFloors,
                Capacity = 12
            },
            new ElevatorConfig
            {
                Label = "B",
                InitialFloor = minFloor + (maxFloor - minFloor) / 3,
                Type = ElevatorType.Local,
                ServedFloors = null, // All floors
                Capacity = 10
            },
            new ElevatorConfig
            {
                Label = "C",
                InitialFloor = minFloor + 2 * (maxFloor - minFloor) / 3,
                Type = ElevatorType.Local,
                ServedFloors = null, // All floors
                Capacity = 10
            }
        };
    }

    /// <summary>
    /// Create a configuration with 1 Freight and 2 Local elevators
    /// Freight has higher capacity and serves all floors
    /// </summary>
    public static ElevatorConfig[] CreateFreightLocalMix(int minFloor, int maxFloor)
    {
        return new[]
        {
            new ElevatorConfig
            {
                Label = "F1",
                InitialFloor = minFloor,
                Type = ElevatorType.Freight,
                ServedFloors = null, // All floors
                Capacity = 20 // Higher capacity
            },
            new ElevatorConfig
            {
                Label = "L1",
                InitialFloor = minFloor + (maxFloor - minFloor) / 2,
                Type = ElevatorType.Local,
                ServedFloors = null,
                Capacity = 10
            },
            new ElevatorConfig
            {
                Label = "L2",
                InitialFloor = maxFloor,
                Type = ElevatorType.Local,
                ServedFloors = null,
                Capacity = 10
            }
        };
    }

    /// <summary>
    /// Set access restriction for a specific floor
    /// </summary>
    public void SetFloorRestriction(int floor, FloorRestriction restriction)
    {
        if (floor < _minFloor || floor > _maxFloor)
        {
            throw new ArgumentOutOfRangeException(
                nameof(floor),
                $"Floor must be between {_minFloor} and {_maxFloor}");
        }

        _floorRestrictions[floor] = restriction;
    }

    /// <summary>
    /// Check if a request has access to a specific floor
    /// </summary>
    public bool CanAccessFloor(Request request, int floor)
    {
        // Check if there's a restriction for this floor
        if (!_floorRestrictions.TryGetValue(floor, out var restriction))
        {
            // No restriction - check access level's allowed floors
            if (request.AccessLevel.AllowedFloors != null &&
                !request.AccessLevel.AllowedFloors.Contains(floor))
            {
                return false;
            }
            return true;
        }

        // Floor has restrictions - check VIP requirement
        if (restriction.RequiresVIP && !request.AccessLevel.IsVIP)
        {
            return false;
        }

        // Check specific access level restrictions
        if (restriction.AllowedAccessLevels.Any() &&
            !restriction.AllowedAccessLevels.Contains(request.AccessLevel.Name))
        {
            return false;
        }

        // Check access level's allowed floors
        if (request.AccessLevel.AllowedFloors != null &&
            !request.AccessLevel.AllowedFloors.Contains(floor))
        {
            return false;
        }

        return true;
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

        // Validate floor access permissions
        if (!CanAccessFloor(request, request.PickupFloor))
        {
            throw new UnauthorizedAccessException(
                $"Access denied to pickup floor {request.PickupFloor} for access level '{request.AccessLevel.Name}'");
        }

        if (!CanAccessFloor(request, request.DestinationFloor))
        {
            throw new UnauthorizedAccessException(
                $"Access denied to destination floor {request.DestinationFloor} for access level '{request.AccessLevel.Name}'");
        }

        _requests.Enqueue(request);
        _performanceTracker.RecordRequest(request);
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
            // Use system-level targets instead of elevator queue
            var targets = GetElevatorTargets(i).ToList();

            string targetDisplay;
            if (!targets.Any())
            {
                targetDisplay = "None";
            }
            else if (targets.Count == 1)
            {
                var target = targets[0];
                var direction = target > elevator.CurrentFloor ? "↑" : target < elevator.CurrentFloor ? "↓" : "•";
                var distance = Math.Abs(target - elevator.CurrentFloor);
                targetDisplay = $"{target}{direction} ({distance} floors away)";
            }
            else
            {
                var nextTarget = targets[0];
                var direction = nextTarget > elevator.CurrentFloor ? "↑" : nextTarget < elevator.CurrentFloor ? "↓" : "•";
                var distance = Math.Abs(nextTarget - elevator.CurrentFloor);
                var queuedTargets = string.Join(", ", targets.Skip(1));
                targetDisplay = $"Next: {nextTarget}{direction} ({distance}) → Queue: [{queuedTargets}]";
            }

            status.AppendLine($"Elevator {GetElevatorLabel(i)} [{elevator.Type}]: Floor {elevator.CurrentFloor,-2} | {elevator.State,-12} | {targetDisplay}");
        }

        status.AppendLine();
        status.AppendLine($"Pending Requests: {PendingRequestCount}");

        // Show priority breakdown if requests exist
        if (PendingRequestCount > 0)
        {
            var requestsList = _requests.ToArray();
            var priorityCounts = requestsList
                .GroupBy(r => r.Priority)
                .OrderByDescending(g => g.Key)
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToArray();

            if (priorityCounts.Any())
            {
                status.AppendLine($"  Priority breakdown: {string.Join(", ", priorityCounts)}");
            }
        }

        return status.ToString();
    }

    /// <summary>
    /// Get current performance metrics
    /// </summary>
    public PerformanceMetrics GetPerformanceMetrics()
    {
        return _performanceTracker.GetMetrics();
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
        return Algorithm switch
        {
            DispatchAlgorithm.Simple => FindBestElevatorSimple(request),
            DispatchAlgorithm.SCAN => FindBestElevatorScan(request),
            DispatchAlgorithm.LOOK => FindBestElevatorLook(request),
            _ => FindBestElevatorSimple(request)
        };
    }

    private int? FindBestElevatorSimple(Request request)
    {
        lock (_dispatchLock)
        {
            // Separate elevators into idle and busy groups
            var idleElevators = new List<(int index, int distance)>();
            var busyElevators = new List<(int index, int distance)>();

            for (int i = 0; i < _elevators.Count; i++)
            {
                var elevator = _elevators[i];

                // Skip elevators in maintenance
                if (elevator.InMaintenance)
                    continue;

                // Skip elevators that can't serve pickup or destination floor
                if (!elevator.CanServeFloor(request.PickupFloor) ||
                    !elevator.CanServeFloor(request.DestinationFloor))
                    continue;

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

            // For HIGH priority, ignore idle preference and return absolutely closest
            if (request.Priority == RequestPriority.High)
            {
                var allElevators = idleElevators.Concat(busyElevators);
                if (allElevators.Any())
                {
                    var best = allElevators.OrderBy(e => e.distance).First();
                    return best.index;
                }
                return null;
            }

            // For NORMAL priority, maintain existing idle preference logic
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

    private int? FindBestElevatorScan(Request request)
    {
        lock (_dispatchLock)
        {
            var scores = new List<(int index, double score)>();

            for (int i = 0; i < _elevators.Count; i++)
            {
                var elevator = _elevators[i];

                // Skip elevators in maintenance
                if (elevator.InMaintenance)
                    continue;

                // Skip elevators that can't serve pickup or destination floor
                if (!elevator.CanServeFloor(request.PickupFloor) ||
                    !elevator.CanServeFloor(request.DestinationFloor))
                    continue;

                double score = 0;

                // High priority: ignore direction bonuses, just pick closest
                if (request.Priority == RequestPriority.High)
                {
                    score = -CalculateDistance(elevator.CurrentFloor, request.PickupFloor);
                    scores.Add((i, score));
                    continue;
                }

                // SCAN: Elevator moving in same direction and will pass by pickup floor gets big bonus
                if (IsElevatorHeadingToward(elevator, request.PickupFloor))
                {
                    score += SCAN_DIRECTION_BONUS;
                }

                // Distance penalty (negative score for distance)
                score -= CalculateDistance(elevator.CurrentFloor, request.PickupFloor);

                // Idle bonus
                if (elevator.State == ElevatorState.IDLE)
                {
                    score += SCAN_IDLE_BONUS;
                }

                scores.Add((i, score));
            }

            if (!scores.Any())
                return null;

            // Return elevator with highest score
            return scores.OrderByDescending(s => s.score).First().index;
        }
    }

    private int? FindBestElevatorLook(Request request)
    {
        lock (_dispatchLock)
        {
            var scores = new List<(int index, double score)>();

            for (int i = 0; i < _elevators.Count; i++)
            {
                var elevator = _elevators[i];

                // Skip elevators in maintenance
                if (elevator.InMaintenance)
                    continue;

                // Skip elevators that can't serve pickup or destination floor
                if (!elevator.CanServeFloor(request.PickupFloor) ||
                    !elevator.CanServeFloor(request.DestinationFloor))
                    continue;

                double score = 0;

                // High priority: ignore direction bonuses, just pick closest
                if (request.Priority == RequestPriority.High)
                {
                    score = -CalculateDistance(elevator.CurrentFloor, request.PickupFloor);
                    scores.Add((i, score));
                    continue;
                }

                // LOOK: Similar to SCAN but with slightly less aggressive direction preference
                // since LOOK reverses at last request rather than building end
                if (IsElevatorHeadingToward(elevator, request.PickupFloor))
                {
                    score += LOOK_DIRECTION_BONUS;
                }

                // Distance penalty
                score -= CalculateDistance(elevator.CurrentFloor, request.PickupFloor);

                // Idle bonus
                if (elevator.State == ElevatorState.IDLE)
                {
                    score += LOOK_IDLE_BONUS;
                }

                scores.Add((i, score));
            }

            if (!scores.Any())
                return null;

            // Return elevator with highest score
            return scores.OrderByDescending(s => s.score).First().index;
        }
    }

    private bool IsElevatorHeadingToward(Elevator elevator, int targetFloor)
    {
        // Check if elevator is moving toward target floor
        if (elevator.State == ElevatorState.MOVING_UP && targetFloor > elevator.CurrentFloor)
            return true;
        if (elevator.State == ElevatorState.MOVING_DOWN && targetFloor < elevator.CurrentFloor)
            return true;

        return false;
    }

    private static string GetElevatorLabel(int index)
    {
        // Convert 0->A, 1->B, 2->C, 3->D, 4->E
        return ((char)('A' + index)).ToString();
    }

    public void AssignRequest(int elevatorIndex, Request request)
    {
        if (elevatorIndex < 0 || elevatorIndex >= _elevators.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(elevatorIndex));
        }

        var elevator = _elevators[elevatorIndex];

        // Track this request for completion detection
        var progress = new RequestProgress
        {
            RequestId = request.RequestId,
            PickupFloor = request.PickupFloor,
            DestinationFloor = request.DestinationFloor,
            Priority = request.Priority,
            AssignedElevatorIndex = elevatorIndex,
            PickupReached = false,
            DestinationReached = false,
            RequestTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // Populate pending floors for this request
        progress.PendingFloors.Enqueue(request.PickupFloor);
        progress.PendingFloors.Enqueue(request.DestinationFloor);

        _activeRequests[request.RequestId] = progress;

        // Add pickup floor first, then destination floor to system-level target queue
        // This ensures the elevator goes to pickup the passenger before going to their destination
        lock (_targetLock)
        {
            _elevatorTargets[elevatorIndex].Enqueue(request.PickupFloor);
            _elevatorTargets[elevatorIndex].Enqueue(request.DestinationFloor);
        }

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

        // Main dispatcher loop - process incoming requests with priority sorting
        while (!cancellationToken.IsCancellationRequested)
        {
            // Dequeue all pending requests for priority sorting
            var pendingRequests = new List<Request>();
            while (_requests.TryDequeue(out var req))
            {
                pendingRequests.Add(req);
            }

            if (pendingRequests.Any())
            {
                // Sort by priority (descending) then timestamp (ascending)
                var sortedRequests = pendingRequests
                    .OrderByDescending(r => r.Priority)
                    .ThenBy(r => r.Timestamp)
                    .ToList();

                // Process highest priority request
                var request = sortedRequests.First();

                // Track dispatch time
                var dispatchStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var bestElevatorIndex = FindBestElevator(request);
                dispatchStopwatch.Stop();
                _performanceTracker.RecordDispatchTime(dispatchStopwatch.Elapsed);

                if (bestElevatorIndex.HasValue)
                {
                    AssignRequest(bestElevatorIndex.Value, request);
                }
                else
                {
                    // No elevator available - re-enqueue the request
                    _requests.Enqueue(request);
                    Console.WriteLine($"[SYSTEM] No elevator available, re-queueing {request}");
                }

                // Re-enqueue remaining requests
                foreach (var remainingRequest in sortedRequests.Skip(1))
                {
                    _requests.Enqueue(remainingRequest);
                }
            }

            // Small delay to prevent tight loop
            await Task.Delay(REQUEST_LOOP_DELAY_MS, cancellationToken);
        }

        // Wait for all elevator tasks to complete
        await Task.WhenAll(_elevatorTasks);
    }

    private async Task ProcessElevatorAsync(int elevatorIndex, CancellationToken cancellationToken)
    {
        var elevator = _elevators[elevatorIndex];
        var elevatorLabel = GetElevatorLabel(elevatorIndex);

        while (!cancellationToken.IsCancellationRequested)
        {
            // Use system-level target retrieval instead of elevator queue
            if (GetNextTargetForElevator(elevatorIndex, out int targetFloor))
            {
                var startFloor = elevator.CurrentFloor;

                // Move to target floor
                while (elevator.CurrentFloor != targetFloor && !cancellationToken.IsCancellationRequested)
                {
                    if (elevator.CurrentFloor < targetFloor)
                    {
                        var moveStart = System.Diagnostics.Stopwatch.StartNew();
                        await elevator.MoveUp();
                        moveStart.Stop();
                        _performanceTracker.RecordElevatorMovement(elevatorLabel, 1, moveStart.Elapsed);
                    }
                    else if (elevator.CurrentFloor > targetFloor)
                    {
                        var moveStart = System.Diagnostics.Stopwatch.StartNew();
                        await elevator.MoveDown();
                        moveStart.Stop();
                        _performanceTracker.RecordElevatorMovement(elevatorLabel, 1, moveStart.Elapsed);
                    }
                }

                // Open and close doors at target floor
                if (!cancellationToken.IsCancellationRequested)
                {
                    var doorStart = System.Diagnostics.Stopwatch.StartNew();
                    await elevator.OpenDoor();
                    await elevator.CloseDoor();
                    doorStart.Stop();
                    _performanceTracker.RecordElevatorDoorTime(elevatorLabel, doorStart.Elapsed);

                    Console.WriteLine($"[ELEVATOR {elevatorLabel}] Arrived at floor {targetFloor}");

                    // Track trip completion
                    _performanceTracker.RecordCompletedTrip(elevatorLabel);

                    // Check if this floor completes any requests
                    CheckRequestCompletion(elevatorIndex, targetFloor);
                }
            }
            else
            {
                // No targets, track idle time
                var idleStart = System.Diagnostics.Stopwatch.StartNew();
                await Task.Delay(IDLE_DELAY_MS, cancellationToken);
                idleStart.Stop();
                _performanceTracker.RecordElevatorIdleTime(elevatorLabel, idleStart.Elapsed);
            }
        }
    }

    private void CheckRequestCompletion(int elevatorIndex, int reachedFloor)
    {
        // Find all active requests assigned to this elevator
        foreach (var kvp in _activeRequests)
        {
            var progress = kvp.Value;

            // Only check requests assigned to this elevator
            if (progress.AssignedElevatorIndex != elevatorIndex)
                continue;

            // Check if this floor is the pickup floor
            if (reachedFloor == progress.PickupFloor && !progress.PickupReached)
            {
                progress.PickupReached = true;
                progress.PickupTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Console.WriteLine($"[TRACKING] Request #{progress.RequestId}: Pickup complete at floor {reachedFloor}");
            }

            // Check if this floor is the destination floor
            if (reachedFloor == progress.DestinationFloor && !progress.DestinationReached)
            {
                progress.DestinationReached = true;
                progress.DestinationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Console.WriteLine($"[TRACKING] Request #{progress.RequestId}: Destination complete at floor {reachedFloor}");
            }

            // If both pickup and destination are reached, mark as complete
            if (progress.IsComplete)
            {
                _completedRequestIds.Add(progress.RequestId);
                _activeRequests.TryRemove(progress.RequestId, out _);
                Console.WriteLine($"[TRACKING] Request #{progress.RequestId}: FULLY COMPLETE");

                // Track performance metrics
                if (progress.PickupTimestamp.HasValue && progress.DestinationTimestamp.HasValue)
                {
                    var waitTime = TimeSpan.FromMilliseconds(progress.PickupTimestamp.Value - progress.RequestTimestamp);
                    var rideTime = TimeSpan.FromMilliseconds(progress.DestinationTimestamp.Value - progress.PickupTimestamp.Value);
                    _performanceTracker.RecordCompletedRequest(waitTime, rideTime);
                }
            }
        }
    }

    public List<int> GetCompletedRequestIds()
    {
        return _completedRequestIds.ToList();
    }

    public void ClearCompletedRequestIds()
    {
        _completedRequestIds.Clear();
    }


    // System-level target management methods
    public bool GetNextTargetForElevator(int elevatorIndex, out int targetFloor)
    {
        lock (_targetLock)
        {
            if (_elevatorTargets.TryGetValue(elevatorIndex, out var queue) &&
                queue.Count > 0)
            {
                targetFloor = queue.Dequeue();
                return true;
            }
            targetFloor = 0;
            return false;
        }
    }

    public IEnumerable<int> GetElevatorTargets(int elevatorIndex)
    {
        lock (_targetLock)
        {
            if (_elevatorTargets.TryGetValue(elevatorIndex, out var queue))
            {
                return queue.ToArray();
            }
            return Array.Empty<int>();
        }
    }

    public void Dispose()
    {
        foreach (var logWriter in _elevatorLogs.Values)
        {
            logWriter?.Dispose();
        }
        _elevatorLogs.Clear();
    }
}
