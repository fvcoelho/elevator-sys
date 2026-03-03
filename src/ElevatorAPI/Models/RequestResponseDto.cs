namespace ElevatorAPI.Models;

public record RequestResponseDto(
    int RequestId,
    int PickupFloor,
    int DestinationFloor,
    string Direction,
    string Priority,
    string AccessLevel,
    string? PreferredElevatorType);
