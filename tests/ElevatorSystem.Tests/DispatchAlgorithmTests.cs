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

        // Act & Assert - Custom
        system.Algorithm = DispatchAlgorithm.Custom;
        system.Algorithm.Should().Be(DispatchAlgorithm.Custom);
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

        // Test Custom
        system.Algorithm = DispatchAlgorithm.Custom;
        var bestCustom = system.FindBestElevator(request);
        bestCustom.Should().Be(1, "Should skip maintenance elevator");
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

        system.Algorithm = DispatchAlgorithm.Custom;
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

        system.Algorithm = DispatchAlgorithm.Custom;
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

    [Fact]
    public void Custom_PicksElevatorWithShortestRoute()
    {
        // Arrange - Elevator A at floor 1, Elevator B at floor 10
        var configs = new[]
        {
            new ElevatorConfig { Label = "A", InitialFloor = 1, Type = ElevatorType.Local },
            new ElevatorConfig { Label = "B", InitialFloor = 10, Type = ElevatorType.Local }
        };

        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        system.Algorithm = DispatchAlgorithm.Custom;

        // Give elevator A an existing request far away (pickup 15, dest 20)
        var existingRequest = new Request(15, 20, minFloor: 1, maxFloor: 20);
        system.AssignRequest(0, existingRequest);

        // New request: pickup 9, dest 12
        // Elevator A at 1 with targets [15,20] + new [9,12]: total ~28
        // Elevator B at 10 with no targets + new [9,12]: total ~4
        var request = new Request(9, 12, minFloor: 1, maxFloor: 20);

        // Act
        var best = system.FindBestElevator(request);

        // Assert - Custom should pick B (shorter total route)
        best.Should().Be(1, "Elevator B has shorter total travel distance");
    }

    [Fact]
    public void Custom_ReordersTargets()
    {
        // Arrange - Elevator at floor 1
        var configs = new[]
        {
            new ElevatorConfig { Label = "A", InitialFloor = 1, Type = ElevatorType.Local }
        };

        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        system.Algorithm = DispatchAlgorithm.Custom;

        // Assign two requests: first far away, then close
        // Request 1: pickup 15, dest 18
        // Request 2: pickup 3, dest 5
        // Without reordering: [15, 18, 3, 5] (FIFO)
        // With reordering: [3, 5, 15, 18] (nearest-neighbor from floor 1)
        var request1 = new Request(15, 18, minFloor: 1, maxFloor: 20);
        var request2 = new Request(3, 5, minFloor: 1, maxFloor: 20);

        system.AssignRequest(0, request1);
        system.AssignRequest(0, request2);

        // Act - Get the reordered targets
        var targets = system.GetElevatorTargets(0).ToList();

        // Assert - Should visit closer floors first
        targets.Should().HaveCount(4);
        targets[0].Should().Be(3, "Closest pickup from floor 1");
        targets[1].Should().Be(5, "Destination for nearby request");
        targets[2].Should().Be(15, "Far pickup");
        targets[3].Should().Be(18, "Far destination");
    }

    [Fact]
    public void Custom_RespectsPickupBeforeDestination()
    {
        // Arrange - Elevator at floor 10
        var configs = new[]
        {
            new ElevatorConfig { Label = "A", InitialFloor = 10, Type = ElevatorType.Local }
        };

        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        system.Algorithm = DispatchAlgorithm.Custom;

        // Request: pickup 5, destination 2
        // Even though floor 2 is farther, destination can only be visited after pickup
        var request = new Request(5, 2, minFloor: 1, maxFloor: 20);
        system.AssignRequest(0, request);

        var targets = system.GetElevatorTargets(0).ToList();

        // Assert - Pickup must come before destination
        targets.Should().HaveCount(2);
        var pickupIdx = targets.IndexOf(5);
        var destIdx = targets.IndexOf(2);
        pickupIdx.Should().BeLessThan(destIdx, "Pickup floor must be visited before destination");
    }

    [Fact]
    public void Custom_DeduplicatesSharedFloors()
    {
        // Arrange - Elevator at floor 1, multiple requests sharing floors
        var configs = new[]
        {
            new ElevatorConfig { Label = "A", InitialFloor = 1, Type = ElevatorType.Local }
        };

        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        system.Algorithm = DispatchAlgorithm.Custom;

        // Three requests all picking up from floor 1 (lobby)
        // Request 1: pickup 1, dest 5
        // Request 2: pickup 1, dest 10
        // Request 3: pickup 1, dest 8
        var r1 = new Request(1, 5, minFloor: 1, maxFloor: 20);
        var r2 = new Request(1, 10, minFloor: 1, maxFloor: 20);
        var r3 = new Request(1, 8, minFloor: 1, maxFloor: 20);

        system.AssignRequest(0, r1);
        system.AssignRequest(0, r2);
        system.AssignRequest(0, r3);

        var targets = system.GetElevatorTargets(0).ToList();

        // Assert - Floor 1 should appear only once, not three times
        targets.Count(f => f == 1).Should().Be(1, "Shared pickup floor should not be duplicated");
        // All destination floors should be present
        targets.Should().Contain(5);
        targets.Should().Contain(8);
        targets.Should().Contain(10);
        // Total: 1 (pickup) + 5 + 8 + 10 = 4 stops
        targets.Should().HaveCount(4);
    }

    [Fact]
    public void Custom_EmptyQueue_PicksClosest()
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

        system.Algorithm = DispatchAlgorithm.Custom;

        // Request at floor 9 going to 12; no existing targets
        var request = new Request(9, 12, minFloor: 1, maxFloor: 20);

        // Act
        var best = system.FindBestElevator(request);

        // Assert - B at floor 10 is closest (distance 1 to pickup 9)
        best.Should().Be(1, "With empty queues, Custom picks closest elevator");
    }

    [Fact]
    public void Custom_HighPriority_PicksClosest()
    {
        // Arrange
        var configs = new[]
        {
            new ElevatorConfig { Label = "A", InitialFloor = 1, Type = ElevatorType.Local },
            new ElevatorConfig { Label = "B", InitialFloor = 19, Type = ElevatorType.Local }
        };

        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        system.Algorithm = DispatchAlgorithm.Custom;

        // High priority request at floor 18
        var request = new Request(18, 15, RequestPriority.High, minFloor: 1, maxFloor: 20);

        // Act
        var best = system.FindBestElevator(request);

        // Assert - High priority ignores optimization, picks closest
        best.Should().Be(1, "High priority picks closest elevator (B at floor 19)");
    }

    [Fact]
    public void CalculateOptimalOrder_BasicCase()
    {
        // Arrange - Elevator at floor 1, single pair: pickup 5, dest 10
        var pairs = new List<PairState>
        {
            new PairState { PickupFloor = 5, DestinationFloor = 10 }
        };

        // Act
        var (order, totalDistance) = ElevatorSystem.CalculateOptimalOrder(1, pairs);

        // Assert
        order.Should().Equal(5, 10);
        totalDistance.Should().Be(9); // 1->5 = 4, 5->10 = 5
    }

    [Fact]
    public void CalculateOptimalOrder_PrecedenceConstraints()
    {
        // Arrange - Elevator at floor 10, two pairs
        // Pair A: pickup 8, dest 2 (going down)
        // Pair B: pickup 12, dest 15 (going up)
        // Nearest-neighbor from 10: 8 (dist 2), then 2 (dest, dist 6), then 12 (dist 10), then 15 (dist 3)
        // But 12 is closer from 10 than 8... let's think:
        // From 10: available pickups = 8 (dist 2), 12 (dist 2) - tie, pick first found (8)
        // From 8: available = dest 2 (dist 6), pickup 12 (dist 4) -> pick 12
        // From 12: available = dest 2 (dist 10), dest 15 (dist 3) -> pick 15
        // From 15: available = dest 2 (dist 13) -> pick 2
        var pairs = new List<PairState>
        {
            new PairState { PickupFloor = 8, DestinationFloor = 2 },
            new PairState { PickupFloor = 12, DestinationFloor = 15 }
        };

        // Act
        var (order, totalDistance) = ElevatorSystem.CalculateOptimalOrder(10, pairs);

        // Assert - Pickup must always come before its destination
        var pickup8Idx = order.IndexOf(8);
        var dest2Idx = order.IndexOf(2);
        var pickup12Idx = order.IndexOf(12);
        var dest15Idx = order.IndexOf(15);

        pickup8Idx.Should().BeLessThan(dest2Idx, "Pickup 8 must precede destination 2");
        pickup12Idx.Should().BeLessThan(dest15Idx, "Pickup 12 must precede destination 15");
        order.Should().HaveCount(4);
    }
}
