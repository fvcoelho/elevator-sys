using ElevatorAPI.Models;

namespace ElevatorAPI.Endpoints;

public static class StatusEndpoints
{
    public static RouteGroupBuilder MapStatusEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/status", (ElevatorSystem.ElevatorSystem system) =>
        {
            var elevators = BuildElevatorDtos(system);
            var status = new SystemStatusDto(
                system.ElevatorCount,
                system.PendingRequestCount,
                system.IsEmergencyStopped,
                system.Algorithm.ToString(),
                elevators);

            return Results.Ok(status);
        })
        .WithName("GetStatus")
        .Produces<SystemStatusDto>();

        group.MapGet("/elevators", (ElevatorSystem.ElevatorSystem system) =>
        {
            return Results.Ok(BuildElevatorDtos(system));
        })
        .WithName("GetElevators")
        .Produces<List<ElevatorDto>>();

        group.MapGet("/elevators/{index:int}", (int index, ElevatorSystem.ElevatorSystem system) =>
        {
            try
            {
                var dto = BuildElevatorDto(system, index);
                return Results.Ok(dto);
            }
            catch (ArgumentOutOfRangeException)
            {
                return Results.NotFound(new ErrorResponse("Elevator not found", $"Index {index} is out of range (0-{system.ElevatorCount - 1})"));
            }
        })
        .WithName("GetElevator")
        .Produces<ElevatorDto>()
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return group;
    }

    internal static List<ElevatorDto> BuildElevatorDtos(ElevatorSystem.ElevatorSystem system)
    {
        var elevators = new List<ElevatorDto>();
        for (int i = 0; i < system.ElevatorCount; i++)
        {
            elevators.Add(BuildElevatorDto(system, i));
        }
        return elevators;
    }

    internal static ElevatorDto BuildElevatorDto(ElevatorSystem.ElevatorSystem system, int index)
    {
        var elevator = system.GetElevator(index);
        var targets = system.GetElevatorTargets(index).ToArray();

        return new ElevatorDto(
            index,
            elevator.Label,
            elevator.CurrentFloor,
            elevator.State.ToString(),
            elevator.Type.ToString(),
            elevator.InMaintenance,
            elevator.InEmergencyStop,
            elevator.Capacity,
            elevator.ServedFloors?.ToArray(),
            targets);
    }
}
