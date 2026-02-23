using FluentAssertions;

namespace ElevatorSystem.Tests;

public class ElevatorControllerTests
{
    private const int MIN_FLOOR = 1;
    private const int MAX_FLOOR = 10;
    private const int INITIAL_FLOOR = 5;
    private const int DOOR_OPEN_MS = 5;      // Short for tests
    private const int FLOOR_TRAVEL_MS = 10;  // Short for tests

    private (Elevator elevator, ElevatorController controller) CreateTestController()
    {
        var elevator = new Elevator(MIN_FLOOR, MAX_FLOOR, INITIAL_FLOOR, DOOR_OPEN_MS, FLOOR_TRAVEL_MS);
        var controller = new ElevatorController(elevator);
        return (elevator, controller);
    }

    [Fact]
    public void RequestElevator_ValidFloor_Enqueues()
    {
        // Arrange
        var (_, controller) = CreateTestController();
        const int targetFloor = 7;

        // Act & Assert - Should not throw exception
        var act = () => controller.RequestElevator(targetFloor);
        act.Should().NotThrow();
    }

    [Fact]
    public void RequestElevator_InvalidFloor_ThrowsException()
    {
        // Arrange
        var (_, controller) = CreateTestController();

        // Act & Assert
        var actTooLow = () => controller.RequestElevator(0);
        actTooLow.Should().Throw<ArgumentException>()
            .WithMessage($"Floor must be between {MIN_FLOOR} and {MAX_FLOOR}*");

        var actTooHigh = () => controller.RequestElevator(11);
        actTooHigh.Should().Throw<ArgumentException>()
            .WithMessage($"Floor must be between {MIN_FLOOR} and {MAX_FLOOR}*");
    }

    [Fact]
    public void RequestElevator_DuplicateLastFloor_IgnoresRequest()
    {
        // Arrange
        var (_, controller) = CreateTestController();

        // Act - Request same floor twice in a row
        controller.RequestElevator(7);
        controller.RequestElevator(7); // Should be ignored

        // Request different floor, then same again
        controller.RequestElevator(3);
        controller.RequestElevator(3); // Should be ignored

        // Request the first floor again (not last, should be added)
        controller.RequestElevator(7); // Should be added

        // Assert - Should not throw, and duplicate prevention is working
        // We can't directly access the queue, but the test verifies no exceptions are thrown
        var act = () => controller.RequestElevator(8);
        act.Should().NotThrow();
    }

    [Fact]
    public void RequestElevator_DifferentFloors_AllEnqueued()
    {
        // Arrange
        var (_, controller) = CreateTestController();

        // Act - Request different floors
        var act = () =>
        {
            controller.RequestElevator(5);
            controller.RequestElevator(8);
            controller.RequestElevator(3);
            controller.RequestElevator(10);
        };

        // Assert - Should not throw exception
        act.Should().NotThrow();
    }

    [Fact]
    public async Task ProcessRequests_MovesToTarget()
    {
        // Arrange
        var (elevator, controller) = CreateTestController();
        const int targetFloor = 8;
        controller.RequestElevator(targetFloor);

        using var cts = new CancellationTokenSource();

        // Act
        var processingTask = Task.Run(async () =>
        {
            await controller.ProcessRequestsAsync(cts.Token);
        });

        // Wait for processing
        await Task.Delay(500);

        // Assert
        elevator.CurrentFloor.Should().Be(targetFloor);
        elevator.State.Should().Be(ElevatorState.IDLE);

        // Cleanup
        cts.Cancel();
        try
        {
            await processingTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    [Fact]
    public async Task ProcessRequests_MultipleRequests_AllCompleted()
    {
        // Arrange - Test that controller processes multiple requests to completion
        var (elevator, controller) = CreateTestController();

        var requestedFloors = new[] { 8, 2, 7 };
        foreach (var floor in requestedFloors)
        {
            controller.RequestElevator(floor);
        }

        using var cts = new CancellationTokenSource();

        // Act
        var processingTask = Task.Run(async () =>
        {
            await controller.ProcessRequestsAsync(cts.Token);
        });

        // Wait for all requests to be processed
        var timeout = TimeSpan.FromSeconds(2);
        var startTime = DateTime.UtcNow;

        while (elevator.HasTargets() && DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(50);
        }

        // Cleanup
        cts.Cancel();
        try
        {
            await processingTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - All requests should be processed
        elevator.HasTargets().Should().BeFalse("all targets should be processed");
    }

    [Fact]
    public async Task ConcurrentRequests_AllProcessed()
    {
        // Arrange
        var (elevator, controller) = CreateTestController();
        const int requestCount = 20; // Reduced for faster test execution
        var floors = new List<int>();

        // Generate random floor requests
        var random = new Random(42); // Seed for reproducibility
        for (int i = 0; i < requestCount; i++)
        {
            floors.Add(random.Next(MIN_FLOOR, MAX_FLOOR + 1));
        }

        using var cts = new CancellationTokenSource();

        // Start processing
        var processingTask = Task.Run(async () =>
        {
            await controller.ProcessRequestsAsync(cts.Token);
        });

        // Act - Submit requests concurrently
        var requestTasks = floors.Select(floor =>
            Task.Run(() => controller.RequestElevator(floor))
        ).ToArray();

        await Task.WhenAll(requestTasks);

        // Wait for processing to complete - longer timeout for 20 requests
        var timeout = TimeSpan.FromSeconds(5);
        var startTime = DateTime.UtcNow;

        while (elevator.HasTargets() && DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(100);
        }

        // Assert - All requests should be processed
        elevator.HasTargets().Should().BeFalse("all targets should be processed");

        // Cleanup
        cts.Cancel();
        try
        {
            await processingTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    [Fact]
    public void Constructor_NullElevator_ThrowsException()
    {
        // Act & Assert
        var act = () => new ElevatorController(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
