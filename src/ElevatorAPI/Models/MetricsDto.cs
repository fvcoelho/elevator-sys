namespace ElevatorAPI.Models;

public record ElevatorMetricsDto(
    string Label,
    int TripsCompleted,
    int FloorsTraversed,
    double TotalMovingTimeMs,
    double TotalIdleTimeMs,
    double TotalDoorTimeMs,
    double Utilization,
    double AverageFloorsPerTrip);

public record MetricsDto(
    int TotalRequests,
    int CompletedRequests,
    double AverageWaitTimeMs,
    double AverageRideTimeMs,
    double AverageDispatchTimeMs,
    double SystemUtilization,
    int PeakConcurrentRequests,
    Dictionary<string, int> FloorHeatmap,
    Dictionary<string, int> RequestsByPriority,
    int VIPRequests,
    int StandardRequests,
    Dictionary<string, ElevatorMetricsDto> ElevatorStats);
