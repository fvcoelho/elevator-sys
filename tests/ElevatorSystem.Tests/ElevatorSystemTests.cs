using FluentAssertions;

namespace ElevatorSystem.Tests;

public class ElevatorSystemTests
{
    #region Initialization Tests

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Constructor_ValidElevatorCount_CreatesSystem(int elevatorCount)
    {
        // Arrange & Act
        var system = new ElevatorSystem(elevatorCount, minFloor: 1, maxFloor: 20);

        // Assert
        system.ElevatorCount.Should().Be(elevatorCount);
        system.PendingRequestCount.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(10)]
    public void Constructor_InvalidElevatorCount_ThrowsArgumentOutOfRangeException(int elevatorCount)
    {
        // Arrange & Act & Assert
        var act = () => new ElevatorSystem(elevatorCount, minFloor: 1, maxFloor: 20);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Elevator count must be between 1 and 5*");
    }

    [Fact]
    public void Constructor_MinFloorGreaterOrEqualToMaxFloor_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var act = () => new ElevatorSystem(elevatorCount: 3, minFloor: 10, maxFloor: 10);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Min floor must be less than max floor*");
    }

    [Fact]
    public void Constructor_ThreeElevators_DistributedAtFloors1_10_20()
    {
        // Arrange & Act
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);

        // Assert
        system.GetElevator(0).CurrentFloor.Should().Be(1);
        system.GetElevator(1).CurrentFloor.Should().Be(10);
        system.GetElevator(2).CurrentFloor.Should().Be(20);
    }

    [Fact]
    public void Constructor_FiveElevators_DistributedEvenly()
    {
        // Arrange & Act
        var system = new ElevatorSystem(elevatorCount: 5, minFloor: 1, maxFloor: 20);

        // Assert - Should be at floors 1, 5, 10, 15, 20 (approx)
        system.GetElevator(0).CurrentFloor.Should().Be(1);
        system.GetElevator(1).CurrentFloor.Should().BeInRange(5, 6);
        system.GetElevator(2).CurrentFloor.Should().Be(10);
        system.GetElevator(3).CurrentFloor.Should().BeInRange(14, 15);
        system.GetElevator(4).CurrentFloor.Should().Be(20);
    }

    [Fact]
    public void Constructor_AllElevatorsStartAtIdleState()
    {
        // Arrange & Act
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);

        // Assert
        for (int i = 0; i < system.ElevatorCount; i++)
        {
            system.GetElevator(i).State.Should().Be(ElevatorState.IDLE);
        }
    }

    #endregion

    #region Request Management Tests

    [Fact]
    public void AddRequest_ValidRequest_EnqueuesRequest()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);
        var request = new Request(pickupFloor: 5, destinationFloor: 15);

        // Act
        system.AddRequest(request);

        // Assert
        system.PendingRequestCount.Should().Be(1);
    }

    [Fact]
    public void AddRequest_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);

        // Act & Assert
        var act = () => system.AddRequest(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddRequest_PickupFloorOutOfRange_ThrowsArgumentException()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);

        // Create request with pickup floor outside system range
        var request = new Request(pickupFloor: 25, destinationFloor: 30, minFloor: 1, maxFloor: 50);

        // Act & Assert - System should reject floor outside its range (1-20)
        var act = () => system.AddRequest(request);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Pickup floor*outside valid range*");
    }

    [Fact]
    public void AddRequest_DestinationFloorOutOfRange_ThrowsArgumentException()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);

        // Create request with destination floor outside system range
        var request = new Request(pickupFloor: 5, destinationFloor: 25, minFloor: 1, maxFloor: 50);

        // Act & Assert
        var act = () => system.AddRequest(request);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Destination floor*outside valid range*");
    }

    [Fact]
    public void AddRequest_MultipleRequests_AllEnqueued()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);
        var request1 = new Request(pickupFloor: 1, destinationFloor: 10);
        var request2 = new Request(pickupFloor: 5, destinationFloor: 15);
        var request3 = new Request(pickupFloor: 8, destinationFloor: 3);

        // Act
        system.AddRequest(request1);
        system.AddRequest(request2);
        system.AddRequest(request3);

        // Assert
        system.PendingRequestCount.Should().Be(3);
    }

    [Fact]
    public async Task AddRequest_ConcurrentRequests_AllEnqueuedSafely()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);
        const int requestCount = 100;
        var tasks = new Task[requestCount];

        // Act - Add requests concurrently
        for (int i = 0; i < requestCount; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                var request = new Request(
                    pickupFloor: 1 + (index % 10),
                    destinationFloor: 11 + (index % 10));
                system.AddRequest(request);
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        system.PendingRequestCount.Should().Be(requestCount);
    }

    #endregion

    #region GetSystemStatus Tests

    [Fact]
    public void GetSystemStatus_ReturnsFormattedStatus()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);

        // Act
        var status = system.GetSystemStatus();

        // Assert
        status.Should().Contain("ELEVATOR SYSTEM");
        status.Should().Contain("3 elevators");
        status.Should().Contain("floors 1-20");
        status.Should().Contain("Elevator A:");
        status.Should().Contain("Elevator B:");
        status.Should().Contain("Elevator C:");
        status.Should().Contain("Pending Requests: 0");
    }

    [Fact]
    public void GetSystemStatus_WithPendingRequests_ShowsCount()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);
        system.AddRequest(new Request(pickupFloor: 5, destinationFloor: 10));
        system.AddRequest(new Request(pickupFloor: 8, destinationFloor: 15));

        // Act
        var status = system.GetSystemStatus();

        // Assert
        status.Should().Contain("Pending Requests: 2");
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void GetElevator_ValidIndex_ReturnsElevator()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);

        // Act
        var elevator = system.GetElevator(1);

        // Assert
        elevator.Should().NotBeNull();
        elevator.CurrentFloor.Should().Be(10);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(10)]
    public void GetElevator_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);

        // Act & Assert
        var act = () => system.GetElevator(index);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Dispatch Algorithm Tests

    [Fact]
    public void FindBestElevator_AllIdle_ReturnsClosestElevator()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);
        // Elevators at: 1, 10, 20
        var request = new Request(pickupFloor: 12, destinationFloor: 15);

        // Act
        var bestIndex = system.FindBestElevator(request);

        // Assert - Elevator 1 at floor 10 is closest (distance 2)
        bestIndex.Should().Be(1);
    }

    [Fact]
    public async Task FindBestElevator_IdlePreferredOverCloserBusy_ReturnsIdleElevator()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10);
        // Elevators at: 1, 10, 20

        // Make elevator 1 busy by adding a request and processing
        var elevator1 = system.GetElevator(1);
        elevator1.AddRequest(15);

        // Start processing elevator 1 in background to make it busy
        var cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (elevator1.TryGetNextTarget(out int floor))
                {
                    while (elevator1.CurrentFloor != floor)
                    {
                        if (elevator1.CurrentFloor < floor)
                            await elevator1.MoveUp();
                        else
                            await elevator1.MoveDown();
                    }
                    await elevator1.OpenDoor();
                    await elevator1.CloseDoor();
                }
                await Task.Delay(10);
            }
        });

        // Wait a bit for elevator to become busy
        await Task.Delay(50);

        // Act - Request for floor 12 (elevator 1 at 10 is closer, but elevator 2 at 20 is idle)
        var request = new Request(pickupFloor: 12, destinationFloor: 15);
        var bestIndex = system.FindBestElevator(request);

        // Assert - Should prefer idle elevator (0 or 2) over busy elevator 1
        bestIndex.Should().NotBe(1); // Should not pick the busy elevator
        bestIndex.Should().Match(x => x == 0 || x == 2); // Should be one of the idle elevators

        cts.Cancel();
    }

    [Fact]
    public async Task FindBestElevator_AllBusy_ReturnsClosestBusyElevator()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10);

        // Make all elevators busy
        for (int i = 0; i < 3; i++)
        {
            var elevator = system.GetElevator(i);
            // Add a request that's within valid range
            var targetFloor = elevator.CurrentFloor == 20 ? 19 : elevator.CurrentFloor + 1;
            elevator.AddRequest(targetFloor);
        }

        // Start processing all elevators to make them busy
        var cts = new CancellationTokenSource();
        var tasks = new List<Task>();

        for (int i = 0; i < 3; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var elevator = system.GetElevator(index);
                while (!cts.Token.IsCancellationRequested)
                {
                    if (elevator.TryGetNextTarget(out int floor))
                    {
                        while (elevator.CurrentFloor != floor)
                        {
                            if (elevator.CurrentFloor < floor)
                                await elevator.MoveUp();
                            else
                                await elevator.MoveDown();
                        }
                        await elevator.OpenDoor();
                        await elevator.CloseDoor();
                    }
                    await Task.Delay(10);
                }
            }));
        }

        // Wait for elevators to become busy
        await Task.Delay(50);

        // Act - Request for floor 11 (elevator 1 at 10 should be closest)
        var request = new Request(pickupFloor: 11, destinationFloor: 15);
        var bestIndex = system.FindBestElevator(request);

        // Assert - Should pick elevator 1 (closest to floor 11)
        bestIndex.Should().Be(1);

        cts.Cancel();
        await Task.WhenAll(tasks);
    }

    [Fact]
    public void FindBestElevator_MultileIdleAtSameDistance_ReturnsFirst()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);
        // Elevators at: 1, 10, 20

        // Request equidistant from elevators 0 and 1
        var request = new Request(pickupFloor: 5, destinationFloor: 15);
        // Distance to elevator 0 (floor 1): 4
        // Distance to elevator 1 (floor 10): 5
        // Distance to elevator 2 (floor 20): 15

        // Act
        var bestIndex = system.FindBestElevator(request);

        // Assert - Should pick elevator 0 (closest)
        bestIndex.Should().Be(0);
    }

    [Fact]
    public void AssignRequestToElevator_ValidRequest_AddsPickupAndDestinationToQueue()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);
        var request = new Request(pickupFloor: 5, destinationFloor: 15);

        // Act
        system.AssignRequestToElevator(0, request);

        // Assert
        var elevator = system.GetElevator(0);
        var targets = elevator.GetTargets().ToList();
        targets.Should().HaveCount(2);
        targets[0].Should().Be(5);  // Pickup floor first
        targets[1].Should().Be(15); // Destination floor second
    }

    [Fact]
    public void AssignRequestToElevator_InvalidElevatorIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);
        var request = new Request(pickupFloor: 5, destinationFloor: 15);

        // Act & Assert
        var act = () => system.AssignRequestToElevator(10, request);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AssignRequestToElevator_MultipleRequests_AddsInOrder()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);
        var request1 = new Request(pickupFloor: 5, destinationFloor: 15);
        var request2 = new Request(pickupFloor: 8, destinationFloor: 12);

        // Act
        system.AssignRequestToElevator(0, request1);
        system.AssignRequestToElevator(0, request2);

        // Assert
        var elevator = system.GetElevator(0);
        var targets = elevator.GetTargets().ToList();
        targets.Should().HaveCount(4);
        targets[0].Should().Be(5);  // Request 1 pickup
        targets[1].Should().Be(15); // Request 1 destination
        targets[2].Should().Be(8);  // Request 2 pickup
        targets[3].Should().Be(12); // Request 2 destination
    }

    [Fact]
    public void FindBestElevator_EdgeCase_RequestAtElevatorCurrentFloor()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20);
        // Elevators at: 1, 10, 20

        // Request pickup at elevator 1's current floor
        var request = new Request(pickupFloor: 10, destinationFloor: 15);

        // Act
        var bestIndex = system.FindBestElevator(request);

        // Assert - Should pick elevator 1 (distance 0)
        bestIndex.Should().Be(1);
    }

    [Fact]
    public void FindBestElevator_SingleElevator_AlwaysReturnsThatElevator()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 1, minFloor: 1, maxFloor: 20);
        var request = new Request(pickupFloor: 15, destinationFloor: 5);

        // Act
        var bestIndex = system.FindBestElevator(request);

        // Assert
        bestIndex.Should().Be(0);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Integration_SingleRequest_CompletesSuccessfully()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10);
        var request = new Request(pickupFloor: 5, destinationFloor: 15);

        // Act - Start system and add request
        var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => system.ProcessRequestsAsync(cts.Token));

        system.AddRequest(request);

        // Wait for processing (with timeout)
        await Task.Delay(3000);

        // Assert - Request should be processed and elevator should be idle
        var processedSuccessfully = await WaitForSystemIdle(system, TimeSpan.FromSeconds(5));
        processedSuccessfully.Should().BeTrue("All elevators should return to IDLE after processing");

        // Cancel and cleanup
        cts.Cancel();
        try { await processTask; } catch (OperationCanceledException) { }
    }

    [Fact]
    public async Task Integration_MultipleRequests_AllProcessed()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20, doorOpenMs: 5, floorTravelMs: 5);
        var requests = new[]
        {
            new Request(pickupFloor: 3, destinationFloor: 10),
            new Request(pickupFloor: 8, destinationFloor: 15),
            new Request(pickupFloor: 12, destinationFloor: 5)
        };

        // Act
        var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => system.ProcessRequestsAsync(cts.Token));

        foreach (var request in requests)
        {
            system.AddRequest(request);
            await Task.Delay(50); // Small delay between requests
        }

        // Wait for processing
        var processedSuccessfully = await WaitForSystemIdle(system, TimeSpan.FromSeconds(10));

        // Assert
        processedSuccessfully.Should().BeTrue("All requests should be processed");
        system.PendingRequestCount.Should().Be(0);

        // Cancel and cleanup
        cts.Cancel();
        try { await processTask; } catch (OperationCanceledException) { }
    }

    [Fact]
    public async Task Integration_ConcurrentRequests_AllProcessedCorrectly()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20, doorOpenMs: 5, floorTravelMs: 5);
        const int requestCount = 20;

        // Act
        var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => system.ProcessRequestsAsync(cts.Token));

        // Add requests concurrently
        var addTasks = new Task[requestCount];
        for (int i = 0; i < requestCount; i++)
        {
            var index = i;
            addTasks[i] = Task.Run(() =>
            {
                var request = new Request(
                    pickupFloor: 1 + (index % 10),
                    destinationFloor: 11 + (index % 10));
                system.AddRequest(request);
            });
        }

        await Task.WhenAll(addTasks);

        // Wait for all requests to be processed
        var processedSuccessfully = await WaitForSystemIdle(system, TimeSpan.FromSeconds(30));

        // Assert
        processedSuccessfully.Should().BeTrue("All concurrent requests should be processed");
        system.PendingRequestCount.Should().Be(0);

        // Cancel and cleanup
        cts.Cancel();
        try { await processTask; } catch (OperationCanceledException) { }
    }

    [Fact]
    public async Task Integration_DownwardRide_CompletesCorrectly()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10);
        var request = new Request(pickupFloor: 15, destinationFloor: 5);

        // Act
        var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => system.ProcessRequestsAsync(cts.Token));

        system.AddRequest(request);

        // Wait for processing
        var processedSuccessfully = await WaitForSystemIdle(system, TimeSpan.FromSeconds(10));

        // Assert
        processedSuccessfully.Should().BeTrue();
        request.Direction.Should().Be(Direction.DOWN);

        // Cancel and cleanup
        cts.Cancel();
        try { await processTask; } catch (OperationCanceledException) { }
    }

    [Fact]
    public async Task Integration_RequestFromCurrentFloor_HandledCorrectly()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10);
        // Elevator 1 starts at floor 10
        var request = new Request(pickupFloor: 10, destinationFloor: 15);

        // Act
        var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => system.ProcessRequestsAsync(cts.Token));

        system.AddRequest(request);

        // Wait for processing
        var processedSuccessfully = await WaitForSystemIdle(system, TimeSpan.FromSeconds(10));

        // Assert - Should handle pickup at current floor correctly
        processedSuccessfully.Should().BeTrue();

        // Cancel and cleanup
        cts.Cancel();
        try { await processTask; } catch (OperationCanceledException) { }
    }

    [Fact]
    public async Task Integration_SystemStatus_AccurateDuringOperation()
    {
        // Arrange
        var system = new ElevatorSystem(elevatorCount: 3, minFloor: 1, maxFloor: 20, doorOpenMs: 10, floorTravelMs: 10);
        var request = new Request(pickupFloor: 5, destinationFloor: 15);

        // Act
        var cts = new CancellationTokenSource();
        var processTask = Task.Run(() => system.ProcessRequestsAsync(cts.Token));

        system.AddRequest(request);

        // Check status while processing
        await Task.Delay(50);
        var statusDuringProcessing = system.GetSystemStatus();

        // Wait for completion
        await WaitForSystemIdle(system, TimeSpan.FromSeconds(10));

        var statusAfterProcessing = system.GetSystemStatus();

        // Assert
        statusDuringProcessing.Should().Contain("ELEVATOR SYSTEM");
        statusAfterProcessing.Should().Contain("ELEVATOR SYSTEM");
        statusAfterProcessing.Should().Contain("Pending Requests: 0");

        // Cancel and cleanup
        cts.Cancel();
        try { await processTask; } catch (OperationCanceledException) { }
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
                if (elevator.State != ElevatorState.IDLE)
                {
                    allIdle = false;
                }
                if (elevator.HasTargets())
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
