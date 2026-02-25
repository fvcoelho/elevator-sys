using FluentAssertions;
using Xunit;

namespace ElevatorSystem.Tests;

public class EmergencyStopTests
{
    [Fact]
    public void Elevator_StartsNotInEmergencyStop()
    {
        // Arrange & Act
        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 10,
            initialFloor: 1,
            doorOpenMs: 10,
            floorTravelMs: 10);

        // Assert
        elevator.InEmergencyStop.Should().BeFalse();
        elevator.State.Should().Be(ElevatorState.IDLE);
    }

    [Fact]
    public void EmergencyStop_SetsFlagAndState()
    {
        // Arrange
        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 10,
            initialFloor: 1,
            doorOpenMs: 10,
            floorTravelMs: 10);

        // Act
        elevator.EmergencyStop();

        // Assert
        elevator.InEmergencyStop.Should().BeTrue();
        elevator.State.Should().Be(ElevatorState.EMERGENCY_STOP);
    }

    [Fact]
    public void ResumeFromEmergencyStop_ClearsFlagAndSetsIdle()
    {
        // Arrange
        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 10,
            initialFloor: 1,
            doorOpenMs: 10,
            floorTravelMs: 10);
        elevator.EmergencyStop();

        // Act
        elevator.ResumeFromEmergencyStop();

        // Assert
        elevator.InEmergencyStop.Should().BeFalse();
        elevator.State.Should().Be(ElevatorState.IDLE);
    }

    [Fact]
    public void EmergencyStopAll_StopsAllElevators()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 3,
            minFloor: 1,
            maxFloor: 10,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // Act
        system.EmergencyStopAll();

        // Assert
        system.IsEmergencyStopped.Should().BeTrue();
        for (int i = 0; i < system.ElevatorCount; i++)
        {
            system.GetElevator(i).InEmergencyStop.Should().BeTrue();
            system.GetElevator(i).State.Should().Be(ElevatorState.EMERGENCY_STOP);
        }
    }

    [Fact]
    public void ResumeAll_ResumesAllElevators()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 3,
            minFloor: 1,
            maxFloor: 10,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);
        system.EmergencyStopAll();

        // Act
        system.ResumeAll();

        // Assert
        system.IsEmergencyStopped.Should().BeFalse();
        for (int i = 0; i < system.ElevatorCount; i++)
        {
            system.GetElevator(i).InEmergencyStop.Should().BeFalse();
            system.GetElevator(i).State.Should().Be(ElevatorState.IDLE);
        }
    }

    [Fact]
    public void EmergencyStoppedElevator_ExcludedFromDispatch()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 3,
            minFloor: 1,
            maxFloor: 10,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // Put elevator 0 in emergency stop
        system.GetElevator(0).EmergencyStop();

        var request = new Request(
            pickupFloor: 1,
            destinationFloor: 5,
            minFloor: 1,
            maxFloor: 10);

        // Act
        var bestElevatorIndex = system.FindBestElevator(request);

        // Assert - should not select elevator 0 (in emergency stop)
        bestElevatorIndex.Should().NotBe(0);
        bestElevatorIndex.Should().BeOneOf(1, 2);
    }

    [Fact]
    public void AllElevatorsEmergencyStopped_ReturnsNull()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 10,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        system.EmergencyStopAll();

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

    [Fact]
    public void EmergencyStop_DoesNotAffectMaintenanceState()
    {
        // Arrange
        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 10,
            initialFloor: 1,
            doorOpenMs: 10,
            floorTravelMs: 10);

        // Act - put in maintenance first, then emergency stop
        elevator.EnterMaintenance();
        elevator.EmergencyStop();

        // Assert - maintenance flag should still be set
        elevator.InMaintenance.Should().BeTrue();
        elevator.InEmergencyStop.Should().BeTrue();

        // Resume from emergency stop should not clear maintenance
        elevator.ResumeFromEmergencyStop();
        elevator.InMaintenance.Should().BeTrue();
        elevator.InEmergencyStop.Should().BeFalse();
    }
}
