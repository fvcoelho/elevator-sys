namespace ElevatorAPI.Models;

public record ElevatorDto(
    int Index,
    string Label,
    int CurrentFloor,
    string State,
    string Type,
    bool InMaintenance,
    bool InEmergencyStop,
    int Capacity,
    int[]? ServedFloors,
    int[] TargetFloors);
