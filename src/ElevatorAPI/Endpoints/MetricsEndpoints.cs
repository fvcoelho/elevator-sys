using ElevatorAPI.Models;

namespace ElevatorAPI.Endpoints;

public static class MetricsEndpoints
{
    public static RouteGroupBuilder MapMetricsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/metrics", (ElevatorSystem.ElevatorSystem system) =>
        {
            var metrics = system.GetPerformanceMetrics();

            var elevatorStats = metrics.ElevatorStats.ToDictionary(
                kvp => kvp.Key,
                kvp => new ElevatorMetricsDto(
                    kvp.Value.Label,
                    kvp.Value.TripsCompleted,
                    kvp.Value.FloorsTraversed,
                    kvp.Value.TotalMovingTime.TotalMilliseconds,
                    kvp.Value.TotalIdleTime.TotalMilliseconds,
                    kvp.Value.TotalDoorTime.TotalMilliseconds,
                    kvp.Value.Utilization,
                    kvp.Value.AverageFloorsPerTrip));

            var dto = new MetricsDto(
                metrics.TotalRequests,
                metrics.CompletedRequests,
                metrics.AverageWaitTime.TotalMilliseconds,
                metrics.AverageRideTime.TotalMilliseconds,
                metrics.AverageDispatchTime.TotalMilliseconds,
                metrics.SystemUtilization,
                metrics.PeakConcurrentRequests,
                metrics.FloorHeatmap.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
                metrics.RequestsByPriority.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
                metrics.VIPRequests,
                metrics.StandardRequests,
                elevatorStats);

            return Results.Ok(dto);
        })
        .WithName("GetMetrics")
        .Produces<MetricsDto>();

        return group;
    }
}
