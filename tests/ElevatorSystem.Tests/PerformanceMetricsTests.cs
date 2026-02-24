using FluentAssertions;
using Xunit;

namespace ElevatorSystem.Tests;

public class PerformanceMetricsTests
{
    [Fact]
    public void PerformanceTracker_InitializesElevator()
    {
        // Arrange
        var tracker = new PerformanceTracker();

        // Act
        tracker.InitializeElevator("A");
        var metrics = tracker.GetMetrics();

        // Assert
        metrics.ElevatorStats.Should().ContainKey("A");
        metrics.ElevatorStats["A"].Label.Should().Be("A");
        metrics.ElevatorStats["A"].TripsCompleted.Should().Be(0);
    }

    [Fact]
    public void PerformanceTracker_RecordsRequest()
    {
        // Arrange
        var tracker = new PerformanceTracker();
        var request = new Request(1, 10);

        // Act
        tracker.RecordRequest(request);
        var metrics = tracker.GetMetrics();

        // Assert
        metrics.TotalRequests.Should().Be(1);
        metrics.FloorHeatmap[1].Should().Be(1); // Pickup floor
        metrics.FloorHeatmap[10].Should().Be(1); // Destination floor
    }

    [Fact]
    public void PerformanceTracker_RecordsCompletedRequest()
    {
        // Arrange
        var tracker = new PerformanceTracker();
        var waitTime = TimeSpan.FromSeconds(5);
        var rideTime = TimeSpan.FromSeconds(10);

        // Act
        tracker.RecordCompletedRequest(waitTime, rideTime);
        var metrics = tracker.GetMetrics();

        // Assert
        metrics.CompletedRequests.Should().Be(1);
        metrics.AverageWaitTime.Should().BeCloseTo(waitTime, TimeSpan.FromMilliseconds(10));
        metrics.AverageRideTime.Should().BeCloseTo(rideTime, TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public void PerformanceTracker_RecordsDispatchTime()
    {
        // Arrange
        var tracker = new PerformanceTracker();
        var dispatchTime = TimeSpan.FromMilliseconds(5);

        // Act
        tracker.RecordDispatchTime(dispatchTime);
        var metrics = tracker.GetMetrics();

        // Assert
        metrics.AverageDispatchTime.Should().BeCloseTo(dispatchTime, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void PerformanceTracker_RecordsElevatorMovement()
    {
        // Arrange
        var tracker = new PerformanceTracker();
        tracker.InitializeElevator("A");

        // Act
        tracker.RecordElevatorMovement("A", 5, TimeSpan.FromSeconds(7.5));
        var metrics = tracker.GetMetrics();

        // Assert
        metrics.ElevatorStats["A"].FloorsTraversed.Should().Be(5);
        metrics.ElevatorStats["A"].TotalMovingTime.Should().BeCloseTo(TimeSpan.FromSeconds(7.5), TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public void PerformanceTracker_RecordsElevatorIdleTime()
    {
        // Arrange
        var tracker = new PerformanceTracker();
        tracker.InitializeElevator("A");

        // Act
        tracker.RecordElevatorIdleTime("A", TimeSpan.FromSeconds(10));
        var metrics = tracker.GetMetrics();

        // Assert
        metrics.ElevatorStats["A"].TotalIdleTime.Should().BeCloseTo(TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public void PerformanceTracker_CalculatesUtilization()
    {
        // Arrange
        var tracker = new PerformanceTracker();
        tracker.InitializeElevator("A");

        // Act - 6 seconds moving, 4 seconds idle = 60% utilization
        tracker.RecordElevatorMovement("A", 4, TimeSpan.FromSeconds(6));
        tracker.RecordElevatorIdleTime("A", TimeSpan.FromSeconds(4));
        var metrics = tracker.GetMetrics();

        // Assert
        metrics.ElevatorStats["A"].Utilization.Should().BeApproximately(60.0, 0.1);
    }

    [Fact]
    public void PerformanceTracker_RecordsPeakConcurrentRequests()
    {
        // Arrange
        var tracker = new PerformanceTracker();

        // Act - Add 3 requests, complete 1
        tracker.RecordRequest(new Request(1, 5));
        tracker.RecordRequest(new Request(2, 6));
        tracker.RecordRequest(new Request(3, 7));
        tracker.RecordCompletedRequest(TimeSpan.Zero, TimeSpan.Zero);

        var metrics = tracker.GetMetrics();

        // Assert - Peak should be 3
        metrics.PeakConcurrentRequests.Should().Be(3);
    }

    [Fact]
    public void PerformanceTracker_TracksRequestsByPriority()
    {
        // Arrange
        var tracker = new PerformanceTracker();

        // Act
        tracker.RecordRequest(new Request(1, 5, RequestPriority.Normal));
        tracker.RecordRequest(new Request(2, 6, RequestPriority.Normal));
        tracker.RecordRequest(new Request(3, 7, RequestPriority.High));

        var metrics = tracker.GetMetrics();

        // Assert
        metrics.RequestsByPriority[RequestPriority.Normal].Should().Be(2);
        metrics.RequestsByPriority[RequestPriority.High].Should().Be(1);
    }

    [Fact]
    public void PerformanceTracker_TracksVIPRequests()
    {
        // Arrange
        var tracker = new PerformanceTracker();

        // Act
        tracker.RecordRequest(new Request(1, 5, accessLevel: AccessLevel.Standard));
        tracker.RecordRequest(new Request(2, 6, accessLevel: AccessLevel.VIP));
        tracker.RecordRequest(new Request(3, 7, accessLevel: AccessLevel.VIP));

        var metrics = tracker.GetMetrics();

        // Assert
        metrics.VIPRequests.Should().Be(2);
        metrics.StandardRequests.Should().Be(1);
    }

    [Fact]
    public void ElevatorSystem_TracksPerformance()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 10,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // Act - Add some requests
        system.AddRequest(new Request(1, 5));
        system.AddRequest(new Request(3, 8));

        var metrics = system.GetPerformanceMetrics();

        // Assert
        metrics.TotalRequests.Should().Be(2);
        metrics.ElevatorStats.Should().HaveCount(2);
        metrics.FloorHeatmap.Should().ContainKey(1);
        metrics.FloorHeatmap.Should().ContainKey(5);
    }

    [Fact]
    public void ElevatorMetrics_AverageFloorsPerTrip()
    {
        // Arrange
        var metrics = new ElevatorMetrics
        {
            TripsCompleted = 4,
            FloorsTraversed = 20
        };

        // Act & Assert
        metrics.AverageFloorsPerTrip.Should().Be(5.0);
    }

    [Fact]
    public void ElevatorMetrics_ZeroTrips_ReturnsZeroAverage()
    {
        // Arrange
        var metrics = new ElevatorMetrics
        {
            TripsCompleted = 0,
            FloorsTraversed = 0
        };

        // Act & Assert
        metrics.AverageFloorsPerTrip.Should().Be(0);
    }

    [Fact]
    public void PerformanceMetrics_SystemUtilization_AveragesElevators()
    {
        // Arrange
        var metrics = new PerformanceMetrics
        {
            ElevatorStats = new Dictionary<string, ElevatorMetrics>
            {
                ["A"] = new ElevatorMetrics
                {
                    TotalMovingTime = TimeSpan.FromSeconds(60),
                    TotalIdleTime = TimeSpan.FromSeconds(40)
                    // 60% utilization
                },
                ["B"] = new ElevatorMetrics
                {
                    TotalMovingTime = TimeSpan.FromSeconds(80),
                    TotalIdleTime = TimeSpan.FromSeconds(20)
                    // 80% utilization
                }
            }
        };

        // Act & Assert
        metrics.SystemUtilization.Should().BeApproximately(70.0, 0.1); // Average of 60% and 80%
    }

    [Fact]
    public void PerformanceTracker_Reset_ClearsAllMetrics()
    {
        // Arrange
        var tracker = new PerformanceTracker();
        tracker.InitializeElevator("A");
        tracker.RecordRequest(new Request(1, 5));
        tracker.RecordDispatchTime(TimeSpan.FromMilliseconds(5));

        // Act
        tracker.Reset();
        var metrics = tracker.GetMetrics();

        // Assert
        metrics.TotalRequests.Should().Be(0);
        metrics.CompletedRequests.Should().Be(0);
        metrics.ElevatorStats.Should().BeEmpty();
        metrics.FloorHeatmap.Should().BeEmpty();
    }
}
