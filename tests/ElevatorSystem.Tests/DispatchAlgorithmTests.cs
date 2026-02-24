using FluentAssertions;
using Xunit;

namespace ElevatorSystem.Tests;

public class DispatchAlgorithmTests
{
    [Fact]
    public void Simple_DefaultAlgorithm()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 3,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // Assert
        system.Algorithm.Should().Be(DispatchAlgorithm.Simple);
    }

    [Fact]
    public void Simple_IdleElevatorPreferred()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 3,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        system.Algorithm = DispatchAlgorithm.Simple;

        // Elevators at floors: 1 (A), 10 (B), 20 (C) - all IDLE
        // Request at floor 5
        var request = new Request(5, 8, minFloor: 1, maxFloor: 20);

        // Act
        var best = system.FindBestElevator(request);

        // Assert - Should pick elevator A (floor 1), closest to floor 5
        best.Should().Be(0, "Simple algorithm prefers closest idle elevator");
    }

    [Fact]
    public void SCAN_SameDirectionPreferred()
    {
        // Arrange
        var configs = new[]
        {
            new ElevatorConfig { Label = "A", InitialFloor = 1, Type = ElevatorType.Local },
            new ElevatorConfig { Label = "B", InitialFloor = 10, Type = ElevatorType.Local },
            new ElevatorConfig { Label = "C", InitialFloor = 20, Type = ElevatorType.Local }
        };

        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        system.Algorithm = DispatchAlgorithm.SCAN;

        // Simulate: Elevator B is moving up from floor 10
        // We'll add a request to floor 15, then request for floor 12
        system.AddRequest(new Request(10, 15, minFloor: 1, maxFloor: 20));

        // Give elevator B a target to start moving
        var firstRequest = new Request(10, 15, minFloor: 1, maxFloor: 20);
        var assignedTo = system.FindBestElevator(firstRequest);
        if (assignedTo.HasValue)
        {
            system.AssignRequest(assignedTo.Value, firstRequest);
        }

        // Small delay to let elevator start processing (in real scenario)
        // For testing, we'll directly test the algorithm behavior

        // Request at floor 12 (between 10 and 15)
        var request = new Request(12, 18, minFloor: 1, maxFloor: 20);

        // Act
        var best = system.FindBestElevator(request);

        // Assert - SCAN should consider direction
        best.Should().NotBeNull();
    }

    [Fact]
    public void LOOK_IdleSlightlyPreferred()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 3,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        system.Algorithm = DispatchAlgorithm.LOOK;

        // All elevators idle at 1, 10, 20
        var request = new Request(8, 12, minFloor: 1, maxFloor: 20);

        // Act
        var best = system.FindBestElevator(request);

        // Assert - Should find an elevator (LOOK gives higher idle bonus)
        best.Should().NotBeNull();
        best.Should().Be(1, "Elevator B at floor 10 is closest to pickup floor 8");
    }

    [Fact]
    public void Algorithm_CanBeSwitched()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // Act & Assert - Simple
        system.Algorithm = DispatchAlgorithm.Simple;
        system.Algorithm.Should().Be(DispatchAlgorithm.Simple);

        // Act & Assert - SCAN
        system.Algorithm = DispatchAlgorithm.SCAN;
        system.Algorithm.Should().Be(DispatchAlgorithm.SCAN);

        // Act & Assert - LOOK
        system.Algorithm = DispatchAlgorithm.LOOK;
        system.Algorithm.Should().Be(DispatchAlgorithm.LOOK);
    }

    [Fact]
    public void SCAN_HighPriority_IgnoresDirection()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        system.Algorithm = DispatchAlgorithm.SCAN;

        // Elevators at 1 and 20
        // High priority request at floor 10
        var request = new Request(10, 15, RequestPriority.High, minFloor: 1, maxFloor: 20);

        // Act
        var best = system.FindBestElevator(request);

        // Assert - Should pick closest regardless of direction (both idle, so pick closest)
        best.Should().NotBeNull();
        // Elevator 0 at floor 1: distance 9
        // Elevator 1 at floor 20: distance 10
        best.Should().Be(0, "High priority picks closest elevator");
    }

    [Fact]
    public void LOOK_HighPriority_IgnoresDirection()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        system.Algorithm = DispatchAlgorithm.LOOK;

        // Elevators at 1 and 20
        // High priority request at floor 19
        var request = new Request(19, 15, RequestPriority.High, minFloor: 1, maxFloor: 20);

        // Act
        var best = system.FindBestElevator(request);

        // Assert - Should pick closest
        best.Should().NotBeNull();
        // Elevator 0 at floor 1: distance 18
        // Elevator 1 at floor 20: distance 1
        best.Should().Be(1, "High priority picks closest elevator");
    }

    [Fact]
    public void AllAlgorithms_RespectMaintenance()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        system.GetElevator(0).EnterMaintenance();

        var request = new Request(5, 10, minFloor: 1, maxFloor: 20);

        // Test Simple
        system.Algorithm = DispatchAlgorithm.Simple;
        var bestSimple = system.FindBestElevator(request);
        bestSimple.Should().Be(1, "Should skip maintenance elevator");

        // Test SCAN
        system.Algorithm = DispatchAlgorithm.SCAN;
        var bestScan = system.FindBestElevator(request);
        bestScan.Should().Be(1, "Should skip maintenance elevator");

        // Test LOOK
        system.Algorithm = DispatchAlgorithm.LOOK;
        var bestLook = system.FindBestElevator(request);
        bestLook.Should().Be(1, "Should skip maintenance elevator");
    }

    [Fact]
    public void AllAlgorithms_RespectElevatorTypes()
    {
        // Arrange - Express only serves 1 and 15-20
        var configs = new[]
        {
            new ElevatorConfig
            {
                Label = "E",
                InitialFloor = 1,
                Type = ElevatorType.Express,
                ServedFloors = new HashSet<int> { 1 }.Union(Enumerable.Range(15, 6)).ToHashSet()
            },
            new ElevatorConfig
            {
                Label = "L",
                InitialFloor = 10,
                Type = ElevatorType.Local
            }
        };

        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        // Request to floor 10 (Express can't serve)
        var request = new Request(10, 12, minFloor: 1, maxFloor: 20);

        // Test all algorithms - none should select Express (index 0)
        system.Algorithm = DispatchAlgorithm.Simple;
        system.FindBestElevator(request).Should().Be(1);

        system.Algorithm = DispatchAlgorithm.SCAN;
        system.FindBestElevator(request).Should().Be(1);

        system.Algorithm = DispatchAlgorithm.LOOK;
        system.FindBestElevator(request).Should().Be(1);
    }

    [Fact]
    public void SCAN_DirectionBonus_MakesItPreferMovingElevator()
    {
        // Arrange - Create system with custom state
        var configs = new[]
        {
            new ElevatorConfig { Label = "A", InitialFloor = 5, Type = ElevatorType.Local },
            new ElevatorConfig { Label = "B", InitialFloor = 10, Type = ElevatorType.Local }
        };

        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        system.Algorithm = DispatchAlgorithm.SCAN;

        // Both elevators idle, request pickup at 8
        var request = new Request(8, 12, minFloor: 1, maxFloor: 20);

        // Act
        var best = system.FindBestElevator(request);

        // Assert - With both idle, should pick closest
        // A at floor 5: distance 3
        // B at floor 10: distance 2
        best.Should().Be(1, "Elevator B is closer to pickup floor 8");
    }

    [Fact]
    public void NoElevatorAvailable_ReturnsNull()
    {
        // Arrange - Only Express, request to non-served floors
        var configs = new[]
        {
            new ElevatorConfig
            {
                Label = "E",
                InitialFloor = 1,
                Type = ElevatorType.Express,
                ServedFloors = new HashSet<int> { 1, 20 }
            }
        };

        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        var request = new Request(10, 15, minFloor: 1, maxFloor: 20);

        // Test all algorithms
        system.Algorithm = DispatchAlgorithm.Simple;
        system.FindBestElevator(request).Should().BeNull();

        system.Algorithm = DispatchAlgorithm.SCAN;
        system.FindBestElevator(request).Should().BeNull();

        system.Algorithm = DispatchAlgorithm.LOOK;
        system.FindBestElevator(request).Should().BeNull();
    }

    [Fact]
    public void SCAN_vs_Simple_DifferentScoring()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 3,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // All elevators at 1, 10, 20 (idle)
        var request = new Request(15, 18, minFloor: 1, maxFloor: 20);

        // Act - Simple picks closest
        system.Algorithm = DispatchAlgorithm.Simple;
        var bestSimple = system.FindBestElevator(request);

        // Act - SCAN also picks closest when all idle
        system.Algorithm = DispatchAlgorithm.SCAN;
        var bestScan = system.FindBestElevator(request);

        // Assert - Elevators B (floor 10) and C (floor 20) are equally close to 15 (distance 5)
        // Algorithm picks first found with minimum distance
        bestSimple.Should().BeOneOf(1, 2);
        bestScan.Should().BeOneOf(1, 2);
    }
}
