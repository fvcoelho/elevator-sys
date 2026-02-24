using FluentAssertions;

namespace ElevatorSystem.Tests;

public class RequestTests
{
    [Fact]
    public void Constructor_ValidFloorsUpDirection_CreatesRequestWithUpDirection()
    {
        // Arrange & Act
        var request = new Request(pickupFloor: 3, destinationFloor: 10);

        // Assert
        request.PickupFloor.Should().Be(3);
        request.DestinationFloor.Should().Be(10);
        request.Direction.Should().Be(Direction.UP);
        request.RequestId.Should().BeGreaterThan(0);
        request.Timestamp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Constructor_ValidFloorsDownDirection_CreatesRequestWithDownDirection()
    {
        // Arrange & Act
        var request = new Request(pickupFloor: 15, destinationFloor: 5);

        // Assert
        request.PickupFloor.Should().Be(15);
        request.DestinationFloor.Should().Be(5);
        request.Direction.Should().Be(Direction.DOWN);
        request.RequestId.Should().BeGreaterThan(0);
        request.Timestamp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Constructor_PickupFloorBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        var act = () => new Request(pickupFloor: 0, destinationFloor: 10, minFloor: 1, maxFloor: 20);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Pickup floor must be between 1 and 20*");
    }

    [Fact]
    public void Constructor_PickupFloorAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        var act = () => new Request(pickupFloor: 21, destinationFloor: 10, minFloor: 1, maxFloor: 20);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Pickup floor must be between 1 and 20*");
    }

    [Fact]
    public void Constructor_DestinationFloorBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        var act = () => new Request(pickupFloor: 5, destinationFloor: 0, minFloor: 1, maxFloor: 20);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Destination floor must be between 1 and 20*");
    }

    [Fact]
    public void Constructor_DestinationFloorAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        var act = () => new Request(pickupFloor: 5, destinationFloor: 21, minFloor: 1, maxFloor: 20);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Destination floor must be between 1 and 20*");
    }

    [Fact]
    public void Constructor_SamePickupAndDestination_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var act = () => new Request(pickupFloor: 10, destinationFloor: 10);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Pickup floor and destination floor must be different*");
    }

    [Fact]
    public void Constructor_GeneratesUniqueRequestIds()
    {
        // Arrange & Act
        var request1 = new Request(pickupFloor: 1, destinationFloor: 5);
        var request2 = new Request(pickupFloor: 2, destinationFloor: 6);
        var request3 = new Request(pickupFloor: 3, destinationFloor: 7);

        // Assert
        request1.RequestId.Should().NotBe(request2.RequestId);
        request2.RequestId.Should().NotBe(request3.RequestId);
        request1.RequestId.Should().NotBe(request3.RequestId);
    }

    [Fact]
    public async Task Constructor_ThreadSafeIdGeneration_GeneratesUniqueIdsUnderConcurrency()
    {
        // Arrange
        const int requestCount = 1000;
        var requests = new Request[requestCount];
        var tasks = new Task[requestCount];

        // Act - Create requests concurrently
        for (int i = 0; i < requestCount; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                requests[index] = new Request(
                    pickupFloor: 1 + (index % 10),
                    destinationFloor: 11 + (index % 10));
            });
        }

        await Task.WhenAll(tasks);

        // Assert - All IDs should be unique
        var requestIds = requests.Select(r => r.RequestId).ToList();
        requestIds.Should().OnlyHaveUniqueItems();
        requestIds.Should().HaveCount(requestCount);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var request = new Request(pickupFloor: 5, destinationFloor: 15);

        // Act
        var result = request.ToString();

        // Assert
        result.Should().Contain($"Request #{request.RequestId}");
        result.Should().Contain("Floor 5 → 15");
        result.Should().Contain("(UP)");
    }

    [Fact]
    public void Timestamp_IsReasonablyRecent()
    {
        // Arrange
        var beforeTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        var request = new Request(pickupFloor: 1, destinationFloor: 10);

        // Assert
        var afterTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        request.Timestamp.Should().BeGreaterThanOrEqualTo(beforeTimestamp);
        request.Timestamp.Should().BeLessThanOrEqualTo(afterTimestamp);
    }

    [Fact]
    public void Constructor_CustomFloorRange_ValidatesCorrectly()
    {
        // Arrange & Act - Valid request within custom range
        var validRequest = new Request(pickupFloor: 5, destinationFloor: 8, minFloor: 1, maxFloor: 10);

        // Assert
        validRequest.PickupFloor.Should().Be(5);
        validRequest.DestinationFloor.Should().Be(8);
    }

    [Fact]
    public void Constructor_CustomFloorRange_ThrowsForInvalidFloors()
    {
        // Arrange & Act & Assert
        var act = () => new Request(pickupFloor: 11, destinationFloor: 5, minFloor: 1, maxFloor: 10);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Pickup floor must be between 1 and 10*");
    }
}
