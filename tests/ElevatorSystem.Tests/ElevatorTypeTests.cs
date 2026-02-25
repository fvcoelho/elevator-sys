using FluentAssertions;
using Xunit;

namespace ElevatorSystem.Tests;

public class ElevatorTypeTests
{
    [Fact]
    public void LocalElevator_ServesAllFloors()
    {
        // Arrange
        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 20,
            initialFloor: 1,
            doorOpenMs: 10,
            floorTravelMs: 10,
            type: ElevatorType.Local);

        // Assert
        elevator.Type.Should().Be(ElevatorType.Local);

        // Local elevator should serve all floors
        for (int floor = 1; floor <= 20; floor++)
        {
            elevator.CanServeFloor(floor).Should().BeTrue($"Local elevator should serve floor {floor}");
        }
    }

    [Fact]
    public void ExpressElevator_ServesOnlySpecificFloors()
    {
        // Arrange - Express elevator serves floors 1 and 15-20
        var servedFloors = new HashSet<int> { 1 };
        servedFloors.UnionWith(Enumerable.Range(15, 6)); // 15-20

        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 20,
            initialFloor: 1,
            doorOpenMs: 10,
            floorTravelMs: 10,
            type: ElevatorType.Express,
            servedFloors: servedFloors);

        // Assert
        elevator.Type.Should().Be(ElevatorType.Express);
        elevator.CanServeFloor(1).Should().BeTrue();
        elevator.CanServeFloor(15).Should().BeTrue();
        elevator.CanServeFloor(20).Should().BeTrue();

        // Should NOT serve middle floors
        elevator.CanServeFloor(5).Should().BeFalse();
        elevator.CanServeFloor(10).Should().BeFalse();
    }

    [Fact]
    public void FreightElevator_HasHigherCapacity()
    {
        // Arrange
        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 20,
            initialFloor: 1,
            doorOpenMs: 10,
            floorTravelMs: 10,
            type: ElevatorType.Freight,
            capacity: 20);

        // Assert
        elevator.Type.Should().Be(ElevatorType.Freight);
        elevator.Capacity.Should().Be(20);

        // Freight serves all floors by default
        elevator.CanServeFloor(1).Should().BeTrue();
        elevator.CanServeFloor(10).Should().BeTrue();
        elevator.CanServeFloor(20).Should().BeTrue();
    }

    [Fact]
    public void ElevatorConfig_CreatesLocalElevatorByDefault()
    {
        // Arrange
        var config = new ElevatorConfig
        {
            Label = "A",
            InitialFloor = 1
        };

        // Assert
        config.Type.Should().Be(ElevatorType.Local);
        config.Capacity.Should().Be(10);
        config.ServedFloors.Should().BeNull(); // Null means all floors
    }

    [Fact]
    public void CreateExpressLocalMix_Creates1ExpressAnd2Local()
    {
        // Arrange & Act
        var configs = ElevatorSystem.CreateExpressLocalMix(1, 20);

        // Assert
        configs.Should().HaveCount(3);

        // First elevator should be Express
        configs[0].Type.Should().Be(ElevatorType.Express);
        configs[0].ServedFloors.Should().NotBeNull();
        configs[0].ServedFloors!.Should().Contain(1);
        configs[0].ServedFloors!.Should().Contain(15);
        configs[0].ServedFloors!.Should().NotContain(10);

        // Next two should be Local
        configs[1].Type.Should().Be(ElevatorType.Local);
        configs[1].ServedFloors.Should().BeNull();
        configs[2].Type.Should().Be(ElevatorType.Local);
        configs[2].ServedFloors.Should().BeNull();
    }

    [Fact]
    public void CreateFreightLocalMix_Creates1FreightAnd2Local()
    {
        // Arrange & Act
        var configs = ElevatorSystem.CreateFreightLocalMix(1, 20);

        // Assert
        configs.Should().HaveCount(3);

        // First elevator should be Freight
        configs[0].Type.Should().Be(ElevatorType.Freight);
        configs[0].Capacity.Should().Be(20);

        // Next two should be Local
        configs[1].Type.Should().Be(ElevatorType.Local);
        configs[2].Type.Should().Be(ElevatorType.Local);
    }

    [Fact]
    public void ElevatorSystem_WithExpressLocalMix_CreatesCorrectTypes()
    {
        // Arrange
        var configs = ElevatorSystem.CreateExpressLocalMix(1, 20);

        // Act
        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        // Assert
        system.ElevatorCount.Should().Be(3);
        system.GetElevator(0).Type.Should().Be(ElevatorType.Express);
        system.GetElevator(1).Type.Should().Be(ElevatorType.Local);
        system.GetElevator(2).Type.Should().Be(ElevatorType.Local);
    }

    [Fact]
    public void Dispatch_ExpressElevator_OnlyAssignedToServedFloors()
    {
        // Arrange - Express serves only 1 and 15-20, not middle floors
        var configs = ElevatorSystem.CreateExpressLocalMix(1, 20);
        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        // Request to floor 5 (middle floor) should NOT go to Express elevator (index 0)
        var request = new Request(
            pickupFloor: 5,
            destinationFloor: 8,
            minFloor: 1,
            maxFloor: 20);

        // Act
        var bestElevator = system.FindBestElevator(request);

        // Assert - Should be Local elevator (1 or 2), not Express (0)
        bestElevator.Should().NotBe(0);
        bestElevator.Should().BeOneOf(1, 2);
    }

    [Fact]
    public void Dispatch_ExpressElevator_CanBeAssignedToServedFloors()
    {
        // Arrange - Express serves 1 and 15-20
        var configs = ElevatorSystem.CreateExpressLocalMix(1, 20);

        // Put Local elevators in maintenance to force Express selection
        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        system.GetElevator(1).EnterMaintenance();
        system.GetElevator(2).EnterMaintenance();

        // Request to floors Express CAN serve
        var request = new Request(
            pickupFloor: 1,
            destinationFloor: 15,
            minFloor: 1,
            maxFloor: 20);

        // Act
        var bestElevator = system.FindBestElevator(request);

        // Assert - Should be Express elevator (0)
        bestElevator.Should().Be(0);
    }

    [Fact]
    public void Dispatch_NoElevatorCanServeFloors_ReturnsNull()
    {
        // Arrange - Only Express elevator, request to non-served floors
        var configs = new[]
        {
            new ElevatorConfig
            {
                Label = "E1",
                InitialFloor = 1,
                Type = ElevatorType.Express,
                ServedFloors = new HashSet<int> { 1, 20 }, // Only serves 1 and 20
                Capacity = 10
            }
        };

        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        // Request to middle floors Express can't serve
        var request = new Request(
            pickupFloor: 10,
            destinationFloor: 15,
            minFloor: 1,
            maxFloor: 20);

        // Act
        var bestElevator = system.FindBestElevator(request);

        // Assert - Should be null (no elevator can serve)
        bestElevator.Should().BeNull();
    }

    [Fact]
    public void Dispatch_PartialFloorMatch_ReturnsNull()
    {
        // Arrange - Express can serve pickup but not destination
        var configs = new[]
        {
            new ElevatorConfig
            {
                Label = "E1",
                InitialFloor = 1,
                Type = ElevatorType.Express,
                ServedFloors = new HashSet<int> { 1, 15, 16, 17, 18, 19, 20 },
                Capacity = 10
            }
        };

        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        // Request: pickup at 1 (can serve) but destination at 10 (can't serve)
        var request = new Request(
            pickupFloor: 1,
            destinationFloor: 10,
            minFloor: 1,
            maxFloor: 20);

        // Act
        var bestElevator = system.FindBestElevator(request);

        // Assert - Should be null (elevator can't serve destination)
        bestElevator.Should().BeNull();
    }

    [Fact]
    public void GetSystemStatus_ShowsElevatorTypes()
    {
        // Arrange
        var configs = ElevatorSystem.CreateExpressLocalMix(1, 20);
        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        // Act
        var status = system.GetSystemStatus();

        // Assert
        status.Should().Contain("[Express]");
        status.Should().Contain("[Local]");
    }

    [Fact]
    public void FreightElevator_DefaultsToAllFloors()
    {
        // Arrange
        var config = new ElevatorConfig
        {
            Label = "F1",
            InitialFloor = 1,
            Type = ElevatorType.Freight,
            Capacity = 20
            // ServedFloors = null (all floors)
        };

        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 20,
            initialFloor: config.InitialFloor,
            doorOpenMs: 10,
            floorTravelMs: 10,
            type: config.Type,
            servedFloors: config.ServedFloors,
            capacity: config.Capacity);

        // Assert - Freight should serve all floors when ServedFloors is null
        elevator.Type.Should().Be(ElevatorType.Freight);
        for (int floor = 1; floor <= 20; floor++)
        {
            elevator.CanServeFloor(floor).Should().BeTrue();
        }
    }

    [Fact]
    public void Elevator_InitialFloorNotInServedFloors_ThrowsArgumentException()
    {
        // Arrange - Express elevator serves floors 1 and 15-20, but initialFloor=10
        var servedFloors = new HashSet<int> { 1 };
        servedFloors.UnionWith(Enumerable.Range(15, 6));

        // Act & Assert
        var act = () => new Elevator(
            minFloor: 1,
            maxFloor: 20,
            initialFloor: 10,
            doorOpenMs: 10,
            floorTravelMs: 10,
            type: ElevatorType.Express,
            servedFloors: servedFloors);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Initial floor 10*served floors*");
    }

    [Fact]
    public void ElevatorSystem_ConfigInitialFloorNotInServedFloors_ThrowsArgumentException()
    {
        // Arrange - Config with InitialFloor not in ServedFloors
        var configs = new[]
        {
            new ElevatorConfig
            {
                Label = "E1",
                InitialFloor = 10,
                Type = ElevatorType.Express,
                ServedFloors = new HashSet<int> { 1, 15, 16, 17, 18, 19, 20 }
            }
        };

        // Act & Assert
        var act = () => new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Elevator 'E1'*InitialFloor 10*not in its ServedFloors*");
    }

    [Fact]
    public void Elevator_InitialFloorInServedFloors_CreatesSuccessfully()
    {
        // Arrange - Express elevator with initialFloor=1, which IS in servedFloors
        var servedFloors = new HashSet<int> { 1 };
        servedFloors.UnionWith(Enumerable.Range(15, 6));

        // Act
        var elevator = new Elevator(
            minFloor: 1,
            maxFloor: 20,
            initialFloor: 1,
            doorOpenMs: 10,
            floorTravelMs: 10,
            type: ElevatorType.Express,
            servedFloors: servedFloors);

        // Assert
        elevator.CurrentFloor.Should().Be(1);
        elevator.Type.Should().Be(ElevatorType.Express);
        elevator.CanServeFloor(1).Should().BeTrue();
        elevator.CanServeFloor(15).Should().BeTrue();
    }

    [Fact]
    public void CreateExpressLocalMix_SmallMaxFloor_ProducesValidConfig()
    {
        // Act - (1, 10) should produce a valid config with dynamic express start
        var configs = ElevatorSystem.CreateExpressLocalMix(1, 10);

        // Assert
        configs.Should().HaveCount(3);
        configs[0].Type.Should().Be(ElevatorType.Express);
        configs[0].ServedFloors.Should().NotBeNull();
        configs[0].ServedFloors!.Should().Contain(1, "Express should serve lobby");

        // expressStartFloor = 1 + (int)(10 * 0.7) = 1 + 7 = 8
        configs[0].ServedFloors!.Should().Contain(8);
        configs[0].ServedFloors!.Should().Contain(9);
        configs[0].ServedFloors!.Should().Contain(10);

        // InitialFloor must be in ServedFloors
        configs[0].ServedFloors!.Should().Contain(configs[0].InitialFloor);

        // Local elevators should have null ServedFloors
        configs[1].ServedFloors.Should().BeNull();
        configs[2].ServedFloors.Should().BeNull();
    }

    [Fact]
    public void CreateExpressLocalMix_TooSmallRange_ThrowsArgumentException()
    {
        // Act & Assert - (1, 3) is only 3 floors, not enough for express/local mix
        var act = () => ElevatorSystem.CreateExpressLocalMix(1, 3);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*at least 4 floors*");
    }

    [Fact]
    public void MixedElevatorSystem_DispatchesCorrectly()
    {
        // Arrange - 1 Express (1, 15-20), 1 Local (all), 1 Freight (all)
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
            },
            new ElevatorConfig
            {
                Label = "F",
                InitialFloor = 20,
                Type = ElevatorType.Freight,
                Capacity = 20
            }
        };

        var system = new ElevatorSystem(
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10,
            elevatorConfigs: configs);

        // Test 1: Middle floor request - Express can't serve
        var middleRequest = new Request(10, 12, minFloor: 1, maxFloor: 20);
        var bestForMiddle = system.FindBestElevator(middleRequest);
        bestForMiddle.Should().NotBe(0, "Express (0) can't serve middle floors");
        bestForMiddle.Should().BeOneOf(1, 2);

        // Test 2: High floor request - All can serve, should find an elevator
        var highRequest = new Request(15, 18, minFloor: 1, maxFloor: 20);
        var bestForHigh = system.FindBestElevator(highRequest);
        bestForHigh.Should().NotBeNull("At least one elevator should be found");
        bestForHigh.Should().BeGreaterThanOrEqualTo(0);
    }
}
