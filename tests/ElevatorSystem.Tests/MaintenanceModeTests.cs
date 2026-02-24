using FluentAssertions;
using Xunit;

namespace ElevatorSystem.Tests;

public class MaintenanceModeTests
{
    [Fact]
    public void Elevator_StartsNotInMaintenance()
    {
        // Arrange & Act
        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 10,
            initialFloor: 1,
            doorOpenMs: 10,
            floorTravelMs: 10);

        // Assert
        elevator.InMaintenance.Should().BeFalse();
        elevator.State.Should().Be(ElevatorState.IDLE);
    }

    [Fact]
    public void EnterMaintenance_SetsMaintenanceFlagAndState()
    {
        // Arrange
        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 10,
            initialFloor: 1,
            doorOpenMs: 10,
            floorTravelMs: 10);

        // Act
        elevator.EnterMaintenance();

        // Assert
        elevator.InMaintenance.Should().BeTrue();
        elevator.State.Should().Be(ElevatorState.MAINTENANCE);
    }

    [Fact]
    public void ExitMaintenance_ClearsMaintenanceFlagAndSetsIdle()
    {
        // Arrange
        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 10,
            initialFloor: 1,
            doorOpenMs: 10,
            floorTravelMs: 10);
        elevator.EnterMaintenance();

        // Act
        elevator.ExitMaintenance();

        // Assert
        elevator.InMaintenance.Should().BeFalse();
        elevator.State.Should().Be(ElevatorState.IDLE);
    }

    [Fact]
    public async Task DoorOpening_ShowsTransitionState()
    {
        // Arrange
        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 10,
            initialFloor: 1,
            doorOpenMs: 10,
            floorTravelMs: 10);

        // Act - start door opening but don't await
        var openTask = elevator.OpenDoor();
        await Task.Delay(50); // Give it time to enter DOOR_OPENING state

        // Assert - during transition, state should be DOOR_OPENING or DOOR_OPEN
        var state = elevator.State;
        (state == ElevatorState.DOOR_OPENING || state == ElevatorState.DOOR_OPEN).Should().BeTrue();

        // Cleanup - wait for door to finish
        await openTask;
    }

    [Fact]
    public async Task DoorClosing_ShowsTransitionState()
    {
        // Arrange
        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 10,
            initialFloor: 1,
            doorOpenMs: 10,
            floorTravelMs: 10);
        await elevator.OpenDoor();

        // Act - start door closing but don't await
        var closeTask = elevator.CloseDoor();
        await Task.Delay(50); // Give it time to enter DOOR_CLOSING state

        // Assert - during transition, state should be DOOR_CLOSING or IDLE
        var state = elevator.State;
        (state == ElevatorState.DOOR_CLOSING || state == ElevatorState.IDLE).Should().BeTrue();

        // Cleanup - wait for door to finish
        await closeTask;
    }

    [Fact]
    public async Task DoorOpenSequence_TransitionsCorrectly()
    {
        // Arrange
        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 10,
            initialFloor: 1,
            doorOpenMs: 10,
            floorTravelMs: 10);
        elevator.State.Should().Be(ElevatorState.IDLE);

        // Act & Assert - open door
        var openTask = elevator.OpenDoor();
        await Task.Delay(50);
        // Should be in DOOR_OPENING or DOOR_OPEN
        await openTask;
        elevator.State.Should().Be(ElevatorState.DOOR_OPEN);

        // Act & Assert - close door
        var closeTask = elevator.CloseDoor();
        await Task.Delay(50);
        // Should be in DOOR_CLOSING or IDLE
        await closeTask;
        elevator.State.Should().Be(ElevatorState.IDLE);
    }

    [Fact]
    public void MaintenanceElevator_ExcludedFromDispatch()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 3,
            minFloor: 1,
            maxFloor: 10,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // Put elevator 0 in maintenance
        system.GetElevator(0).EnterMaintenance();

        var request = new Request(
            pickupFloor: 5,
            destinationFloor: 8,
            minFloor: 1,
            maxFloor: 10);

        // Act
        var bestElevatorIndex = system.FindBestElevator(request);

        // Assert - should not select elevator 0 (in maintenance)
        bestElevatorIndex.Should().NotBe(0);
        bestElevatorIndex.Should().BeOneOf(1, 2);
    }

    [Fact]
    public void AllElevatorsInMaintenance_ReturnsNull()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 10,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // Put all elevators in maintenance
        system.GetElevator(0).EnterMaintenance();
        system.GetElevator(1).EnterMaintenance();

        var request = new Request(
            pickupFloor: 5,
            destinationFloor: 8,
            minFloor: 1,
            maxFloor: 10);

        // Act
        var bestElevatorIndex = system.FindBestElevator(request);

        // Assert
        bestElevatorIndex.Should().BeNull();
    }
}
