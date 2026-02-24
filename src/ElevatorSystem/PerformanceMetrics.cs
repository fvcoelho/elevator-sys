namespace ElevatorSystem;

/// <summary>
/// Performance metrics for individual elevators
/// </summary>
public class ElevatorMetrics
{
    /// <summary>
    /// Elevator label
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Number of completed trips (pickup + destination = 1 trip)
    /// </summary>
    public int TripsCompleted { get; set; }

    /// <summary>
    /// Total floors traversed
    /// </summary>
    public int FloorsTraversed { get; set; }

    /// <summary>
    /// Total time spent moving between floors
    /// </summary>
    public TimeSpan TotalMovingTime { get; set; }

    /// <summary>
    /// Total time spent idle
    /// </summary>
    public TimeSpan TotalIdleTime { get; set; }

    /// <summary>
    /// Total time spent with doors open/closing
    /// </summary>
    public TimeSpan TotalDoorTime { get; set; }

    /// <summary>
    /// Elevator utilization percentage (moving time / total time)
    /// </summary>
    public double Utilization
    {
        get
        {
            var totalTime = TotalMovingTime + TotalIdleTime + TotalDoorTime;
            if (totalTime.TotalSeconds == 0) return 0;
            return (TotalMovingTime.TotalSeconds / totalTime.TotalSeconds) * 100;
        }
    }

    /// <summary>
    /// Average floors per trip
    /// </summary>
    public double AverageFloorsPerTrip
    {
        get
        {
            if (TripsCompleted == 0) return 0;
            return (double)FloorsTraversed / TripsCompleted;
        }
    }
}

/// <summary>
/// System-wide performance metrics
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Total requests received by the system
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// Number of completed requests
    /// </summary>
    public int CompletedRequests { get; set; }

    /// <summary>
    /// Average wait time (pickup time - request time)
    /// </summary>
    public TimeSpan AverageWaitTime { get; set; }

    /// <summary>
    /// Average ride time (destination time - pickup time)
    /// </summary>
    public TimeSpan AverageRideTime { get; set; }

    /// <summary>
    /// Average dispatch time (time to assign request to elevator)
    /// </summary>
    public TimeSpan AverageDispatchTime { get; set; }

    /// <summary>
    /// Metrics for each elevator
    /// </summary>
    public Dictionary<string, ElevatorMetrics> ElevatorStats { get; set; } = new();

    /// <summary>
    /// System utilization (average of all elevators)
    /// </summary>
    public double SystemUtilization
    {
        get
        {
            if (!ElevatorStats.Any()) return 0;
            return ElevatorStats.Values.Average(e => e.Utilization);
        }
    }

    /// <summary>
    /// Peak concurrent requests handled
    /// </summary>
    public int PeakConcurrentRequests { get; set; }

    /// <summary>
    /// Floor usage frequency (floor number -> request count)
    /// </summary>
    public Dictionary<int, int> FloorHeatmap { get; set; } = new();

    /// <summary>
    /// Requests by priority
    /// </summary>
    public Dictionary<RequestPriority, int> RequestsByPriority { get; set; } = new();

    /// <summary>
    /// VIP vs Standard request counts
    /// </summary>
    public int VIPRequests { get; set; }
    public int StandardRequests { get; set; }
}
