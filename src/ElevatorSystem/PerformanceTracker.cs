using System.Collections.Concurrent;
using System.Diagnostics;

namespace ElevatorSystem;

/// <summary>
/// Tracks performance metrics for the elevator system
/// </summary>
public class PerformanceTracker
{
    private readonly ConcurrentDictionary<string, ElevatorMetrics> _elevatorMetrics = new();
    private readonly ConcurrentBag<TimeSpan> _dispatchTimes = new();
    private readonly ConcurrentBag<TimeSpan> _waitTimes = new();
    private readonly ConcurrentBag<TimeSpan> _rideTimes = new();
    private readonly ConcurrentDictionary<int, int> _floorUsage = new();
    private readonly ConcurrentDictionary<RequestPriority, int> _priorityCounts = new();
    private readonly object _statsLock = new();

    private int _totalRequests;
    private int _completedRequests;
    private int _peakConcurrentRequests;
    private int _currentConcurrentRequests;
    private int _vipRequests;
    private int _standardRequests;

    /// <summary>
    /// Initialize metrics for an elevator
    /// </summary>
    public void InitializeElevator(string label)
    {
        _elevatorMetrics.TryAdd(label, new ElevatorMetrics { Label = label });
    }

    /// <summary>
    /// Record a dispatch time
    /// </summary>
    public void RecordDispatchTime(TimeSpan dispatchTime)
    {
        _dispatchTimes.Add(dispatchTime);
    }

    /// <summary>
    /// Record a new request
    /// </summary>
    public void RecordRequest(Request request)
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Increment(ref _currentConcurrentRequests);

        // Update peak
        lock (_statsLock)
        {
            if (_currentConcurrentRequests > _peakConcurrentRequests)
            {
                _peakConcurrentRequests = _currentConcurrentRequests;
            }
        }

        // Track priority
        _priorityCounts.AddOrUpdate(request.Priority, 1, (_, count) => count + 1);

        // Track VIP vs Standard
        if (request.AccessLevel.IsVIP)
        {
            Interlocked.Increment(ref _vipRequests);
        }
        else
        {
            Interlocked.Increment(ref _standardRequests);
        }

        // Track floor usage
        _floorUsage.AddOrUpdate(request.PickupFloor, 1, (_, count) => count + 1);
        _floorUsage.AddOrUpdate(request.DestinationFloor, 1, (_, count) => count + 1);
    }

    /// <summary>
    /// Record a completed request
    /// </summary>
    public void RecordCompletedRequest(TimeSpan waitTime, TimeSpan rideTime)
    {
        Interlocked.Increment(ref _completedRequests);
        Interlocked.Decrement(ref _currentConcurrentRequests);
        _waitTimes.Add(waitTime);
        _rideTimes.Add(rideTime);
    }

    /// <summary>
    /// Record elevator movement
    /// </summary>
    public void RecordElevatorMovement(string label, int floorsTraversed, TimeSpan movingTime)
    {
        if (_elevatorMetrics.TryGetValue(label, out var metrics))
        {
            lock (metrics)
            {
                metrics.FloorsTraversed += floorsTraversed;
                metrics.TotalMovingTime += movingTime;
            }
        }
    }

    /// <summary>
    /// Record elevator idle time
    /// </summary>
    public void RecordElevatorIdleTime(string label, TimeSpan idleTime)
    {
        if (_elevatorMetrics.TryGetValue(label, out var metrics))
        {
            lock (metrics)
            {
                metrics.TotalIdleTime += idleTime;
            }
        }
    }

    /// <summary>
    /// Record elevator door time
    /// </summary>
    public void RecordElevatorDoorTime(string label, TimeSpan doorTime)
    {
        if (_elevatorMetrics.TryGetValue(label, out var metrics))
        {
            lock (metrics)
            {
                metrics.TotalDoorTime += doorTime;
            }
        }
    }

    /// <summary>
    /// Record a completed trip
    /// </summary>
    public void RecordCompletedTrip(string label)
    {
        if (_elevatorMetrics.TryGetValue(label, out var metrics))
        {
            lock (metrics)
            {
                metrics.TripsCompleted++;
            }
        }
    }

    /// <summary>
    /// Get current performance metrics
    /// </summary>
    public PerformanceMetrics GetMetrics()
    {
        var metrics = new PerformanceMetrics
        {
            TotalRequests = _totalRequests,
            CompletedRequests = _completedRequests,
            PeakConcurrentRequests = _peakConcurrentRequests,
            VIPRequests = _vipRequests,
            StandardRequests = _standardRequests
        };

        // Calculate averages
        if (_dispatchTimes.Any())
        {
            metrics.AverageDispatchTime = TimeSpan.FromMilliseconds(
                _dispatchTimes.Average(t => t.TotalMilliseconds));
        }

        if (_waitTimes.Any())
        {
            metrics.AverageWaitTime = TimeSpan.FromMilliseconds(
                _waitTimes.Average(t => t.TotalMilliseconds));
        }

        if (_rideTimes.Any())
        {
            metrics.AverageRideTime = TimeSpan.FromMilliseconds(
                _rideTimes.Average(t => t.TotalMilliseconds));
        }

        // Copy elevator metrics
        foreach (var kvp in _elevatorMetrics)
        {
            var elevMetrics = kvp.Value;
            lock (elevMetrics)
            {
                metrics.ElevatorStats[kvp.Key] = new ElevatorMetrics
                {
                    Label = elevMetrics.Label,
                    TripsCompleted = elevMetrics.TripsCompleted,
                    FloorsTraversed = elevMetrics.FloorsTraversed,
                    TotalMovingTime = elevMetrics.TotalMovingTime,
                    TotalIdleTime = elevMetrics.TotalIdleTime,
                    TotalDoorTime = elevMetrics.TotalDoorTime
                };
            }
        }

        // Copy floor usage
        metrics.FloorHeatmap = new Dictionary<int, int>(_floorUsage);

        // Copy priority counts
        metrics.RequestsByPriority = new Dictionary<RequestPriority, int>(_priorityCounts);

        return metrics;
    }

    /// <summary>
    /// Reset all metrics
    /// </summary>
    public void Reset()
    {
        _elevatorMetrics.Clear();
        _dispatchTimes.Clear();
        _waitTimes.Clear();
        _rideTimes.Clear();
        _floorUsage.Clear();
        _priorityCounts.Clear();
        _totalRequests = 0;
        _completedRequests = 0;
        _peakConcurrentRequests = 0;
        _currentConcurrentRequests = 0;
        _vipRequests = 0;
        _standardRequests = 0;
    }
}
