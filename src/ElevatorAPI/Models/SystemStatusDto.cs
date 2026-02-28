namespace ElevatorAPI.Models;

public record SystemStatusDto(
    int ElevatorCount,
    int PendingRequests,
    bool IsEmergencyStopped,
    string Algorithm,
    List<ElevatorDto> Elevators);
