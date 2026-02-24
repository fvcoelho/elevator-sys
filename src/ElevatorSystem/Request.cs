namespace ElevatorSystem;

public class Request
{
    private static int _nextRequestId = 0;

    public int RequestId { get; }
    public int PickupFloor { get; }
    public int DestinationFloor { get; }
    public Direction Direction { get; }
    public long Timestamp { get; }

    public Request(int pickupFloor, int destinationFloor, int minFloor = 1, int maxFloor = 20)
    {
        // Validate pickup floor
        if (pickupFloor < minFloor || pickupFloor > maxFloor)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pickupFloor),
                $"Pickup floor must be between {minFloor} and {maxFloor}");
        }

        // Validate destination floor
        if (destinationFloor < minFloor || destinationFloor > maxFloor)
        {
            throw new ArgumentOutOfRangeException(
                nameof(destinationFloor),
                $"Destination floor must be between {minFloor} and {maxFloor}");
        }

        // Validate that pickup and destination are different
        if (pickupFloor == destinationFloor)
        {
            throw new ArgumentException(
                "Pickup floor and destination floor must be different",
                nameof(destinationFloor));
        }

        // Thread-safe ID generation
        RequestId = Interlocked.Increment(ref _nextRequestId);
        PickupFloor = pickupFloor;
        DestinationFloor = destinationFloor;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Auto-calculate direction
        Direction = destinationFloor > pickupFloor ? Direction.UP : Direction.DOWN;
    }

    public override string ToString()
    {
        return $"Request #{RequestId}: Floor {PickupFloor} → {DestinationFloor} ({Direction})";
    }
}
