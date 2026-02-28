using ElevatorAPI.Models;
using ElevatorSystem;

namespace ElevatorAPI.Endpoints;

public static class ControlEndpoints
{
    public static RouteGroupBuilder MapControlEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/dispatch/algorithm", (AlgorithmDto dto, ElevatorSystem.ElevatorSystem system) =>
        {
            if (!Enum.TryParse<DispatchAlgorithm>(dto.Algorithm, ignoreCase: true, out var algorithm))
            {
                return Results.BadRequest(new ErrorResponse(
                    "Invalid algorithm",
                    $"Valid values: {string.Join(", ", Enum.GetNames<DispatchAlgorithm>())}"));
            }

            system.Algorithm = algorithm;
            return Results.Ok(new AlgorithmDto(system.Algorithm.ToString()));
        })
        .WithName("SetAlgorithm")
        .Produces<AlgorithmDto>()
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/emergency/stop", (ElevatorSystem.ElevatorSystem system) =>
        {
            system.EmergencyStopAll();
            return Results.Ok(new { Message = "Emergency stop activated" });
        })
        .WithName("EmergencyStop");

        group.MapPost("/emergency/resume", (ElevatorSystem.ElevatorSystem system) =>
        {
            system.ResumeAll();
            return Results.Ok(new { Message = "System resumed" });
        })
        .WithName("EmergencyResume");

        group.MapPost("/elevators/{index:int}/maintenance", (int index, ElevatorSystem.ElevatorSystem system) =>
        {
            try
            {
                var elevator = system.GetElevator(index);
                if (elevator.InMaintenance)
                {
                    elevator.ExitMaintenance();
                    return Results.Ok(new { Message = $"Elevator {elevator.Label} exited maintenance", InMaintenance = false });
                }
                else
                {
                    elevator.EnterMaintenance();
                    return Results.Ok(new { Message = $"Elevator {elevator.Label} entered maintenance", InMaintenance = true });
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return Results.NotFound(new ErrorResponse("Elevator not found", $"Index {index} is out of range (0-{system.ElevatorCount - 1})"));
            }
        })
        .WithName("ToggleMaintenance")
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return group;
    }
}
