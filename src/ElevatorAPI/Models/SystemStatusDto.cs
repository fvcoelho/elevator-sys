namespace ElevatorAPI.Models;

public record SystemStatusDto(
    int ElevatorCount,
    int PendingRequests,
    bool IsEmergencyStopped,
    string Algorithm,
    int PeopleWaiting,
    int PeopleInTransit,
    long MemoryUsedBytes,
    List<ElevatorDto> Elevators);
