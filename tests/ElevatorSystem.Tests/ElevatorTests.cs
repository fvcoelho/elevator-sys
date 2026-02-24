using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElevatorSystem.Tests;

public class ElevatorTests
{
    private const int MIN_FLOOR = 1;
    private const int MAX_FLOOR = 10;
    private const int INITIAL_FLOOR = 5;
    private const int DOOR_OPEN_MS = 5;      // Short for tests
    private const int FLOOR_TRAVEL_MS = 10;  // Short for tests

    private Elevator CreateTestElevator()
    {
        return new Elevator(MIN_FLOOR, MAX_FLOOR, INITIAL_FLOOR, DOOR_OPEN_MS, FLOOR_TRAVEL_MS, label: "TEST", logger: NullLogger.Instance);
    }

    [Fact]
    public async Task MoveUp_IncreasesFloor()
    {
        // Arrange
        var elevator = CreateTestElevator();
        var initialFloor = elevator.CurrentFloor;

        // Act
        await elevator.MoveUp();

        // Assert
        elevator.CurrentFloor.Should().Be(initialFloor + 1);
    }

    [Fact]
    public async Task MoveDown_DecreasesFloor()
    {
        // Arrange
        var elevator = CreateTestElevator();
        var initialFloor = elevator.CurrentFloor;

        // Act
        await elevator.MoveDown();

        // Assert
        elevator.CurrentFloor.Should().Be(initialFloor - 1);
    }

    [Fact]
    public async Task MoveUp_AtTopFloor_ThrowsException()
    {
        // Arrange
        var elevator = CreateTestElevator();

        // Move to top floor
        while (elevator.CurrentFloor < MAX_FLOOR)
        {
            await elevator.MoveUp();
        }

        // Act & Assert
        var act = async () => await elevator.MoveUp();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Cannot move up from floor {MAX_FLOOR}");
    }

    [Fact]
    public async Task MoveDown_AtBottomFloor_ThrowsException()
    {
        // Arrange
        var elevator = CreateTestElevator();

        // Move to bottom floor
        while (elevator.CurrentFloor > MIN_FLOOR)
        {
            await elevator.MoveDown();
        }

        // Act & Assert
        var act = async () => await elevator.MoveDown();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Cannot move down from floor {MIN_FLOOR}");
    }

    [Fact]
    public async Task OpenDoor_SetsState()
    {
        // Arrange
        var elevator = CreateTestElevator();

        // Act
        await elevator.OpenDoor();

        // Assert - State should be DOOR_OPEN after transition completes
        elevator.State.Should().Be(ElevatorState.DOOR_OPEN);
    }

    [Fact]
    public async Task CloseDoor_ResetsToIdle()
    {
        // Arrange
        var elevator = CreateTestElevator();
        await elevator.OpenDoor();

        // Act
        await elevator.CloseDoor();

        // Assert
        elevator.State.Should().Be(ElevatorState.IDLE);
    }

    [Fact]
    public void Constructor_InvalidInitialFloor_ThrowsException()
    {
        // Act & Assert
        var actTooLow = () => new Elevator(MIN_FLOOR, MAX_FLOOR, 0, DOOR_OPEN_MS, FLOOR_TRAVEL_MS);
        actTooLow.Should().Throw<ArgumentException>()
            .WithMessage($"Initial floor must be between {MIN_FLOOR} and {MAX_FLOOR}*");

        var actTooHigh = () => new Elevator(MIN_FLOOR, MAX_FLOOR, 11, DOOR_OPEN_MS, FLOOR_TRAVEL_MS);
        actTooHigh.Should().Throw<ArgumentException>()
            .WithMessage($"Initial floor must be between {MIN_FLOOR} and {MAX_FLOOR}*");
    }

    [Fact]
    public async Task MoveUp_SetsStateToMovingUp()
    {
        // Arrange
        var elevator = CreateTestElevator();

        // Act
        var moveTask = elevator.MoveUp();

        // Assert - Check state during movement
        elevator.State.Should().Be(ElevatorState.MOVING_UP);

        await moveTask;
    }

    [Fact]
    public async Task MoveDown_SetsStateToMovingDown()
    {
        // Arrange
        var elevator = CreateTestElevator();

        // Act
        var moveTask = elevator.MoveDown();

        // Assert - Check state during movement
        elevator.State.Should().Be(ElevatorState.MOVING_DOWN);

        await moveTask;
    }
}
