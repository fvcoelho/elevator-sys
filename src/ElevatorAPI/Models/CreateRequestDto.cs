namespace ElevatorAPI.Models;

public record CreateRequestDto(
    int PickupFloor,
    int DestinationFloor,
    string? Priority = null,
    string? AccessLevel = null,
    string? PreferredElevatorType = null);
