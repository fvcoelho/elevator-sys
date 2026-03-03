namespace ElevatorAPI.Models;

public record AddElevatorDto(
    string Label,
    int InitialFloor,
    string Type = "Local",
    int Capacity = 10,
    int[]? ServedFloors = null);
