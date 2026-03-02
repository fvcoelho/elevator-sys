namespace ElevatorAPI.Models;

public record UpdateConfigDto(
    int MinFloor,
    int MaxFloor,
    int DoorOpenMs = 3000,
    int FloorTravelMs = 1500,
    int DoorTransitionMs = 1000,
    string Algorithm = "Custom",
    int[] VIPFloors = default!,
    ElevatorConfigDto[] Elevators = default!)
{
    public int[] VIPFloors { get; init; } = VIPFloors ?? [];
    public ElevatorConfigDto[] Elevators { get; init; } = Elevators ?? [];
}

public record ElevatorConfigDto(
    string Label,
    int InitialFloor,
    string Type = "Local",
    int Capacity = 10,
    int[]? ServedFloors = null);
