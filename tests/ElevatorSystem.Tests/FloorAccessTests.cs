using FluentAssertions;
using Xunit;

namespace ElevatorSystem.Tests;

public class FloorAccessTests
{
    [Fact]
    public void StandardAccessLevel_AllowsAllFloors()
    {
        // Arrange
        var accessLevel = AccessLevel.Standard;

        // Assert
        accessLevel.Name.Should().Be("Standard");
        accessLevel.IsVIP.Should().BeFalse();
        accessLevel.AllowedFloors.Should().BeNull("Standard has no floor restrictions");
    }

    [Fact]
    public void VIPAccessLevel_IsVIP()
    {
        // Arrange
        var accessLevel = AccessLevel.VIP;

        // Assert
        accessLevel.Name.Should().Be("VIP");
        accessLevel.IsVIP.Should().BeTrue();
        accessLevel.AllowedFloors.Should().BeNull("VIP can access all floors");
    }

    [Fact]
    public void Request_WithVIPAccess_GetsHighPriority()
    {
        // Arrange & Act
        var request = new Request(
            pickupFloor: 5,
            destinationFloor: 10,
            priority: RequestPriority.Normal,
            accessLevel: AccessLevel.VIP,
            minFloor: 1,
            maxFloor: 20);

        // Assert - VIP automatically gets High priority
        request.Priority.Should().Be(RequestPriority.High);
        request.AccessLevel.IsVIP.Should().BeTrue();
    }

    [Fact]
    public void Request_WithStandardAccess_KeepsNormalPriority()
    {
        // Arrange & Act
        var request = new Request(
            pickupFloor: 5,
            destinationFloor: 10,
            priority: RequestPriority.Normal,
            accessLevel: AccessLevel.Standard,
            minFloor: 1,
            maxFloor: 20);

        // Assert
        request.Priority.Should().Be(RequestPriority.Normal);
        request.AccessLevel.IsVIP.Should().BeFalse();
    }

    [Fact]
    public void Request_DefaultAccessLevel_IsStandard()
    {
        // Arrange & Act - No access level specified
        var request = new Request(5, 10, minFloor: 1, maxFloor: 20);

        // Assert
        request.AccessLevel.Should().NotBeNull();
        request.AccessLevel.Name.Should().Be("Standard");
        request.AccessLevel.IsVIP.Should().BeFalse();
    }

    [Fact]
    public void NoRestrictions_AllRequestsAllowed()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // No restrictions set

        // Act - Standard user request
        var standardRequest = new Request(5, 10, accessLevel: AccessLevel.Standard);
        var act = () => system.AddRequest(standardRequest);

        // Assert
        act.Should().NotThrow();
        system.PendingRequestCount.Should().Be(1);
    }

    [Fact]
    public void VIPOnlyFloor_DeniesStandardAccess()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // Floor 20 is VIP-only
        system.SetFloorRestriction(20, FloorRestriction.VIPOnly(20));

        // Act - Standard user tries to access floor 20
        var standardRequest = new Request(1, 20, accessLevel: AccessLevel.Standard);
        var act = () => system.AddRequest(standardRequest);

        // Assert
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*Access denied to destination floor 20*");
    }

    [Fact]
    public void VIPOnlyFloor_AllowsVIPAccess()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // Floor 20 is VIP-only
        system.SetFloorRestriction(20, FloorRestriction.VIPOnly(20));

        // Act - VIP user accesses floor 20
        var vipRequest = new Request(1, 20, accessLevel: AccessLevel.VIP);
        var act = () => system.AddRequest(vipRequest);

        // Assert
        act.Should().NotThrow();
        system.PendingRequestCount.Should().Be(1);
    }

    [Fact]
    public void CustomAccessLevel_WithFloorRestrictions()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // Create custom access level that can only access floors 1-10
        var limitedAccess = AccessLevel.Create(
            "Limited",
            allowedFloors: Enumerable.Range(1, 10).ToHashSet());

        // Act - Try to access floor 15 (not allowed)
        var request = new Request(5, 15, accessLevel: limitedAccess);
        var act = () => system.AddRequest(request);

        // Assert
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*Access denied to destination floor 15*");
    }

    [Fact]
    public void CustomAccessLevel_AllowedFloors()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // Create custom access level that can access floors 1-10
        var limitedAccess = AccessLevel.Create(
            "Limited",
            allowedFloors: Enumerable.Range(1, 10).ToHashSet());

        // Act - Access floor 8 (allowed)
        var request = new Request(5, 8, accessLevel: limitedAccess);
        var act = () => system.AddRequest(request);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SpecificAccessLevels_OnlyAllowedLevels()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // Floor 15 only allows "Executive" access
        system.SetFloorRestriction(15, FloorRestriction.ForAccessLevels(15, "Executive"));

        var executiveAccess = AccessLevel.Create("Executive");
        var standardAccess = AccessLevel.Standard;

        // Act - Executive can access
        var execRequest = new Request(1, 15, accessLevel: executiveAccess);
        var actExec = () => system.AddRequest(execRequest);
        actExec.Should().NotThrow();

        // Act - Standard cannot access
        var stdRequest = new Request(1, 15, accessLevel: standardAccess);
        var actStd = () => system.AddRequest(stdRequest);

        // Assert
        actStd.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*Access denied to destination floor 15*");
    }

    [Fact]
    public void PickupFloorRestriction_AlsoChecked()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // Floor 18 is VIP-only
        system.SetFloorRestriction(18, FloorRestriction.VIPOnly(18));

        // Act - Standard user tries to pickup from floor 18
        var request = new Request(18, 10, accessLevel: AccessLevel.Standard);
        var act = () => system.AddRequest(request);

        // Assert
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*Access denied to pickup floor 18*");
    }

    [Fact]
    public void MultipleRestrictions_AllEnforced()
    {
        // Arrange
        var system = new ElevatorSystem(
            elevatorCount: 2,
            minFloor: 1,
            maxFloor: 20,
            doorOpenMs: 10,
            floorTravelMs: 10,
            doorTransitionMs: 10);

        // Floor 18, 19, 20 are VIP-only
        system.SetFloorRestriction(18, FloorRestriction.VIPOnly(18));
        system.SetFloorRestriction(19, FloorRestriction.VIPOnly(19));
        system.SetFloorRestriction(20, FloorRestriction.VIPOnly(20));

        var vip = AccessLevel.VIP;
        var standard = AccessLevel.Standard;

        // VIP can access all
        var vipRequest = new Request(1, 18, accessLevel: vip);
        system.AddRequest(vipRequest);

        // Standard cannot access any
        var act18 = () => system.AddRequest(new Request(1, 18, accessLevel: standard));
        var act19 = () => system.AddRequest(new Request(1, 19, accessLevel: standard));
        var act20 = () => system.AddRequest(new Request(1, 20, accessLevel: standard));

        act18.Should().Throw<UnauthorizedAccessException>();
        act19.Should().Throw<UnauthorizedAccessException>();
        act20.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void CreateVIP_FactoryMethod()
    {
        // Arrange & Act
        var vip = AccessLevel.CreateVIP("Executive VIP", Enumerable.Range(15, 6).ToHashSet());

        // Assert
        vip.Name.Should().Be("Executive VIP");
        vip.IsVIP.Should().BeTrue();
        vip.AllowedFloors.Should().NotBeNull();
        vip.AllowedFloors!.Should().Contain(15);
        vip.AllowedFloors!.Should().Contain(20);
    }

    [Fact]
    public void VIPRequest_ToString_ShowsVIP()
    {
        // Arrange
        var request = new Request(5, 10, accessLevel: AccessLevel.VIP);

        // Act
        var str = request.ToString();

        // Assert
        str.Should().Contain("[VIP]");
        str.Should().Contain("[High]"); // VIP gets High priority
    }
}
