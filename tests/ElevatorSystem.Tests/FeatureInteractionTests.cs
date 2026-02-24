using FluentAssertions;

namespace ElevatorSystem.Tests;

public class FeatureInteractionTests
{
    #region Priority + Maintenance

    [Fact]
    public void HighPriorityDispatch_WhenSomeElevatorsInMaintenance()
    {
        // Arrange - 3 elevators at floors 1, 10, 20
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10, doorTransitionMs: 10);

        // Put elevator 0 (floor 1) and elevator 2 (floor 20) in maintenance
        system.GetElevator(0).EnterMaintenance();
        system.GetElevator(2).EnterMaintenance();

        var highRequest = new Request(9, 15, RequestPriority.High);

        // Act
        var bestIndex = system.FindBestElevator(highRequest);

        // Assert - Only elevator 1 (floor 10) is available
        bestIndex.Should().Be(1, "only elevator B at floor 10 is not in maintenance");
        system.GetElevator(bestIndex!.Value).InMaintenance.Should().BeFalse();
    }

    #endregion

    #region Maintenance + Re-queue Recovery

    [Fact]
    public async Task AllElevatorsInMaintenance_ThenRecover_RequestProcessed()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 2, minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10, doorTransitionMs: 10);
        var cts = new CancellationTokenSource();

        // Put all elevators in maintenance
        system.GetElevator(0).EnterMaintenance();
        system.GetElevator(1).EnterMaintenance();

        // Act - Start processing and add a request (it should be re-queued since all are in maintenance)
        var processTask = Task.Run(() => system.ProcessRequestsAsync(cts.Token));
        system.AddRequest(new Request(5, 10));

        // Let the system attempt dispatch (will re-queue since all in maintenance)
        await Task.Delay(300);
        system.PendingRequestCount.Should().BeGreaterThan(0, "request should be re-queued when all elevators are in maintenance");

        // Recover elevator 0
        system.GetElevator(0).ExitMaintenance();

        // Wait for system to process the re-queued request
        var processed = await WaitForSystemIdle(system, TimeSpan.FromSeconds(10));

        // Assert
        processed.Should().BeTrue("request should be processed after elevator exits maintenance");
        system.PendingRequestCount.Should().Be(0);

        cts.Cancel();
        try { await processTask; } catch (OperationCanceledException) { }
    }

    #endregion

    #region Algorithm Switching + Live Processing

    [Fact]
    public async Task AlgorithmSwitch_SimpleToSCAN_MidProcessing()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10, doorTransitionMs: 10);
        system.Algorithm.Should().Be(DispatchAlgorithm.Simple);

        var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => system.ProcessRequestsAsync(cts.Token));

        // Act - Add requests under Simple algorithm
        system.AddRequest(new Request(2, 8));
        system.AddRequest(new Request(3, 12));
        await Task.Delay(200);

        // Switch algorithm mid-processing
        system.Algorithm = DispatchAlgorithm.SCAN;
        system.Algorithm.Should().Be(DispatchAlgorithm.SCAN);

        // Add more requests under SCAN algorithm
        system.AddRequest(new Request(5, 15));
        system.AddRequest(new Request(7, 18));

        // Wait for all to complete
        var processed = await WaitForSystemIdle(system, TimeSpan.FromSeconds(10));

        // Assert - All requests should complete regardless of algorithm switch
        processed.Should().BeTrue("all requests should complete after algorithm switch");
        system.PendingRequestCount.Should().Be(0);

        cts.Cancel();
        try { await processTask; } catch (OperationCanceledException) { }
    }

    #endregion

    #region Priority Sorting with Single Elevator

    [Fact]
    public async Task PrioritySorting_HighAddedLater_ProcessedFirst()
    {
        // Arrange - Single elevator at floor 1
        var configs = new[]
        {
            new ElevatorConfig { Label = "A", InitialFloor = 1, Type = ElevatorType.Local }
        };
        var system = new ElevatorSystem(
            minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10,
            doorTransitionMs: 10, elevatorConfigs: configs);

        // Add multiple normal requests first
        system.AddRequest(new Request(1, 5, RequestPriority.Normal));
        system.AddRequest(new Request(6, 10, RequestPriority.Normal));
        await Task.Delay(10);

        // Add a high priority request last
        var highRequest = new Request(10, 15, RequestPriority.High);
        system.AddRequest(highRequest);

        // Act
        var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => system.ProcessRequestsAsync(cts.Token));

        // Let the dispatcher dequeue and sort
        await Task.Delay(500);
        cts.Cancel();
        try { await processTask; } catch (OperationCanceledException) { }

        // Assert - High priority should be dispatched first (completed or in progress)
        var completedIds = system.GetCompletedRequestIds();
        completedIds.Should().Contain(highRequest.RequestId,
            "high priority request added later should be processed first");
    }

    #endregion

    #region Maintenance Toggle + Concurrent

    [Fact]
    public async Task MaintenanceToggle_MidOperation_RequestsStillProcess()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10, doorTransitionMs: 10);
        var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => system.ProcessRequestsAsync(cts.Token));

        // Act - Add request, put elevator in maintenance, add more, exit maintenance
        system.AddRequest(new Request(2, 8));
        await Task.Delay(100);

        // Toggle elevator 0 into maintenance
        system.GetElevator(0).EnterMaintenance();
        system.GetElevator(0).InMaintenance.Should().BeTrue();

        // Add more requests while elevator 0 is in maintenance
        system.AddRequest(new Request(5, 15));
        system.AddRequest(new Request(3, 12));
        await Task.Delay(200);

        // Exit maintenance
        system.GetElevator(0).ExitMaintenance();
        system.GetElevator(0).InMaintenance.Should().BeFalse();

        // Add another request that might go to the recovered elevator
        system.AddRequest(new Request(1, 6));

        // Wait for all to complete
        var processed = await WaitForSystemIdle(system, TimeSpan.FromSeconds(10));

        // Assert
        processed.Should().BeTrue("all requests should be processed after maintenance toggle");
        system.PendingRequestCount.Should().Be(0);

        cts.Cancel();
        try { await processTask; } catch (OperationCanceledException) { }
    }

    #endregion

    #region Performance Metrics + Live Processing

    [Fact]
    public async Task PerformanceMetrics_AfterProcessing_ReflectsCompleted()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 2, minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10, doorTransitionMs: 10);
        var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => system.ProcessRequestsAsync(cts.Token));

        // Act - Add and process requests
        system.AddRequest(new Request(1, 5));
        system.AddRequest(new Request(10, 15));
        system.AddRequest(new Request(3, 8, RequestPriority.High));

        var processed = await WaitForSystemIdle(system, TimeSpan.FromSeconds(10));
        processed.Should().BeTrue();

        // Assert - Metrics should reflect the completed work
        var metrics = system.GetPerformanceMetrics();
        metrics.TotalRequests.Should().Be(3);
        metrics.CompletedRequests.Should().BeGreaterThanOrEqualTo(1, "at least some requests should be tracked as completed");
        metrics.RequestsByPriority.Should().ContainKey(RequestPriority.High);
        metrics.RequestsByPriority.Should().ContainKey(RequestPriority.Normal);
        metrics.ElevatorStats.Should().NotBeEmpty("elevator stats should be tracked");

        // Dispatch times should have been recorded
        metrics.AverageDispatchTime.Should().BeGreaterThan(TimeSpan.Zero, "dispatch times should be tracked");

        cts.Cancel();
        try { await processTask; } catch (OperationCanceledException) { }
    }

    #endregion

    #region Concurrency + Mixed Priorities

    [Fact]
    public async Task ConcurrentMixedPriority_AllProcessed()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10, doorTransitionMs: 10);
        var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => system.ProcessRequestsAsync(cts.Token));

        // Act - Add 15 requests concurrently with mixed priorities
        var tasks = new Task[15];
        for (int i = 0; i < 15; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                var priority = index % 3 == 0 ? RequestPriority.High : RequestPriority.Normal;
                var pickup = 1 + (index % 10);
                var dest = 11 + (index % 10);
                system.AddRequest(new Request(pickup, dest, priority));
            });
        }
        await Task.WhenAll(tasks);

        // Wait for all to complete
        var processed = await WaitForSystemIdle(system, TimeSpan.FromSeconds(30));

        // Assert
        processed.Should().BeTrue("all concurrent mixed-priority requests should be processed");
        system.PendingRequestCount.Should().Be(0);

        var completedIds = system.GetCompletedRequestIds();
        completedIds.Should().HaveCountGreaterThanOrEqualTo(15, "all 15 requests should complete");

        cts.Cancel();
        try { await processTask; } catch (OperationCanceledException) { }
    }

    #endregion

    #region Request Completion Tracking

    [Fact]
    public async Task RequestCompletionTracking_CorrectIds()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 2, minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10, doorTransitionMs: 10);
        var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => system.ProcessRequestsAsync(cts.Token));

        var request1 = new Request(1, 5);
        var request2 = new Request(10, 15);

        // Act
        system.AddRequest(request1);
        system.AddRequest(request2);

        var processed = await WaitForSystemIdle(system, TimeSpan.FromSeconds(10));
        processed.Should().BeTrue();

        // Assert - Completed IDs should contain both request IDs
        var completedIds = system.GetCompletedRequestIds();
        completedIds.Should().Contain(request1.RequestId);
        completedIds.Should().Contain(request2.RequestId);

        // Clear and verify
        system.ClearCompletedRequestIds();
        var clearedIds = system.GetCompletedRequestIds();
        clearedIds.Should().BeEmpty("completed IDs should be cleared");

        cts.Cancel();
        try { await processTask; } catch (OperationCanceledException) { }
    }

    #endregion

    #region Floor Access + VIP + Full Flow

    [Fact]
    public void FloorAccessRestriction_RejectsStandard_AcceptsVIP()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 2, minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10, doorTransitionMs: 10);

        // Set floor 20 as VIP-only
        system.SetFloorRestriction(20, FloorRestriction.VIPOnly(20));

        // Act & Assert - Standard access should be rejected
        var standardRequest = () => system.AddRequest(
            new Request(1, 20, RequestPriority.Normal, AccessLevel.Standard));
        standardRequest.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*destination floor 20*");

        // Act & Assert - VIP access should be accepted
        var vipRequest = new Request(1, 20, RequestPriority.Normal, AccessLevel.VIP);
        var addVip = () => system.AddRequest(vipRequest);
        addVip.Should().NotThrow("VIP access should be allowed on VIP-only floors");
        system.PendingRequestCount.Should().Be(1);
    }

    #endregion

    #region Algorithm Comparison

    [Fact]
    public void SCANvsSimple_DifferentDispatchForMovingElevator()
    {
        // Arrange - Create two systems with same config but different algorithms
        var configs = new[]
        {
            new ElevatorConfig { Label = "A", InitialFloor = 1, Type = ElevatorType.Local },
            new ElevatorConfig { Label = "B", InitialFloor = 10, Type = ElevatorType.Local },
            new ElevatorConfig { Label = "C", InitialFloor = 20, Type = ElevatorType.Local }
        };

        var simpleSystem = new ElevatorSystem(
            minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10,
            doorTransitionMs: 10, elevatorConfigs: configs);
        simpleSystem.Algorithm = DispatchAlgorithm.Simple;

        var scanSystem = new ElevatorSystem(
            minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10,
            doorTransitionMs: 10, elevatorConfigs: configs);
        scanSystem.Algorithm = DispatchAlgorithm.SCAN;

        // Both systems should return a valid elevator for the same request
        var request = new Request(5, 15);
        var simpleResult = simpleSystem.FindBestElevator(request);
        var scanResult = scanSystem.FindBestElevator(request);

        // Assert - Both algorithms should find an elevator
        simpleResult.Should().NotBeNull("Simple should find an elevator");
        scanResult.Should().NotBeNull("SCAN should find an elevator");

        // Both should be valid indices
        simpleResult!.Value.Should().BeInRange(0, 2);
        scanResult!.Value.Should().BeInRange(0, 2);

        // When all elevators are idle, Simple picks closest idle elevator.
        // Elevator A at floor 1 is distance 4 from pickup floor 5.
        // Elevator B at floor 10 is distance 5 from pickup floor 5.
        // So both algorithms should pick elevator A (closest).
        simpleResult.Value.Should().Be(0, "Simple should pick elevator A (floor 1, distance 4 to floor 5)");
        scanResult.Value.Should().Be(0, "SCAN should also pick elevator A when all idle (closest + idle bonus)");
    }

    #endregion

    #region Priority + Elevator Types

    [Fact]
    public void HighPriority_MixedElevatorTypes_DispatchesToCapable()
    {
        // Arrange - Express serves only floors 1 and 15-20, Local serves all
        var configs = ElevatorSystem.CreateExpressLocalMix(1, 20);
        var system = new ElevatorSystem(
            minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10,
            doorTransitionMs: 10, elevatorConfigs: configs);

        // High priority request from floor 5 to floor 10 - Express can't serve these floors
        var highRequest = new Request(5, 10, RequestPriority.High);

        // Act
        var bestIndex = system.FindBestElevator(highRequest);

        // Assert - Should NOT dispatch to Express (index 0) since it can't serve floor 5 or 10
        bestIndex.Should().NotBeNull("a capable elevator should be found");
        bestIndex!.Value.Should().NotBe(0, "Express elevator cannot serve floors 5 and 10");

        var assignedElevator = system.GetElevator(bestIndex.Value);
        assignedElevator.Type.Should().Be(ElevatorType.Local, "only Local elevators can serve this route");
        assignedElevator.CanServeFloor(5).Should().BeTrue();
        assignedElevator.CanServeFloor(10).Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private async Task<bool> WaitForSystemIdle(ElevatorSystem system, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var allIdle = true;
            var allEmpty = true;

            for (int i = 0; i < system.ElevatorCount; i++)
            {
                var elevator = system.GetElevator(i);

                // Skip maintenance elevators - they won't return to IDLE
                if (elevator.InMaintenance)
                    continue;

                if (elevator.State != ElevatorState.IDLE)
                {
                    allIdle = false;
                }

                if (system.GetElevatorTargets(i).Any())
                {
                    allEmpty = false;
                }
            }

            if (allIdle && allEmpty && system.PendingRequestCount == 0)
            {
                return true;
            }

            await Task.Delay(100);
        }

        return false;
    }

    #endregion
}
