using ElevatorAPI.Configuration;
using ElevatorAPI.Models;
using ElevatorAPI.Services;
using ElevatorSystem;

namespace ElevatorAPI.Endpoints;

public static class ControlEndpoints
{
    public static RouteGroupBuilder MapControlEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/dispatch/algorithm", (AlgorithmDto dto, ElevatorSystemHolder holder) =>
        {
            var system = holder.Current;
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

        group.MapPost("/emergency/stop", (ElevatorSystemHolder holder) =>
        {
            holder.Current.EmergencyStopAll();
            return Results.Ok(new { Message = "Emergency stop activated" });
        })
        .WithName("EmergencyStop");

        group.MapPost("/emergency/resume", (ElevatorSystemHolder holder) =>
        {
            holder.Current.ResumeAll();
            return Results.Ok(new { Message = "System resumed" });
        })
        .WithName("EmergencyResume");

        group.MapPost("/elevators/{index:int}/maintenance", (int index, ElevatorSystemHolder holder) =>
        {
            var system = holder.Current;
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

        group.MapPut("/config", async (UpdateConfigDto dto, ElevatorSystemHolder holder) =>
        {
            // Validate floor range
            if (dto.MinFloor >= dto.MaxFloor)
                return Results.BadRequest(new ErrorResponse("Invalid floor range", "MinFloor must be less than MaxFloor"));

            // Validate elevator count
            if (dto.Elevators.Length == 0 || dto.Elevators.Length > 10)
                return Results.BadRequest(new ErrorResponse("Invalid elevator count", "Must have 1-10 elevators"));

            // Validate algorithm
            if (!Enum.TryParse<DispatchAlgorithm>(dto.Algorithm, ignoreCase: true, out _))
                return Results.BadRequest(new ErrorResponse("Invalid algorithm", $"Valid values: {string.Join(", ", Enum.GetNames<DispatchAlgorithm>())}"));

            // Validate each elevator config
            foreach (var elev in dto.Elevators)
            {
                if (!Enum.TryParse<ElevatorType>(elev.Type, ignoreCase: true, out _))
                    return Results.BadRequest(new ErrorResponse("Invalid elevator type", $"Elevator '{elev.Label}': valid types are {string.Join(", ", Enum.GetNames<ElevatorType>())}"));

                if (elev.InitialFloor < dto.MinFloor || elev.InitialFloor > dto.MaxFloor)
                    return Results.BadRequest(new ErrorResponse("Invalid initial floor", $"Elevator '{elev.Label}': InitialFloor must be between {dto.MinFloor} and {dto.MaxFloor}"));
            }

            var options = new ElevatorSystemOptions
            {
                MinFloor = dto.MinFloor,
                MaxFloor = dto.MaxFloor,
                DoorOpenMs = dto.DoorOpenMs,
                FloorTravelMs = dto.FloorTravelMs,
                DoorTransitionMs = dto.DoorTransitionMs,
                Algorithm = dto.Algorithm,
                VIPFloors = dto.VIPFloors,
                Elevators = dto.Elevators.Select(e => new ElevatorConfigOptions
                {
                    Label = e.Label,
                    InitialFloor = e.InitialFloor,
                    Type = e.Type,
                    Capacity = e.Capacity,
                    ServedFloors = e.ServedFloors
                }).ToArray()
            };

            await holder.ResetAsync(options);

            var system = holder.Current;
            var elevators = StatusEndpoints.BuildElevatorDtos(system);
            return Results.Ok(new { Message = "System reconfigured", ElevatorCount = system.ElevatorCount, Elevators = elevators });
        })
        .WithName("UpdateConfig")
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/elevators", async (AddElevatorDto dto, ElevatorSystemHolder holder) =>
        {
            var currentOptions = holder.CurrentOptions;

            // Validate max elevator count
            if (currentOptions.Elevators.Length >= 10)
                return Results.BadRequest(new ErrorResponse("Max elevators reached", "Cannot exceed 10 elevators"));

            // Validate type
            if (!Enum.TryParse<ElevatorType>(dto.Type, ignoreCase: true, out _))
                return Results.BadRequest(new ErrorResponse("Invalid elevator type", $"Valid types: {string.Join(", ", Enum.GetNames<ElevatorType>())}"));

            // Validate initial floor
            if (dto.InitialFloor < currentOptions.MinFloor || dto.InitialFloor > currentOptions.MaxFloor)
                return Results.BadRequest(new ErrorResponse("Invalid initial floor", $"Must be between {currentOptions.MinFloor} and {currentOptions.MaxFloor}"));

            var newElevatorConfig = new ElevatorConfigOptions
            {
                Label = dto.Label,
                InitialFloor = dto.InitialFloor,
                Type = dto.Type,
                Capacity = dto.Capacity,
                ServedFloors = dto.ServedFloors
            };

            var newOptions = new ElevatorSystemOptions
            {
                MinFloor = currentOptions.MinFloor,
                MaxFloor = currentOptions.MaxFloor,
                DoorOpenMs = currentOptions.DoorOpenMs,
                FloorTravelMs = currentOptions.FloorTravelMs,
                DoorTransitionMs = currentOptions.DoorTransitionMs,
                Algorithm = currentOptions.Algorithm,
                VIPFloors = currentOptions.VIPFloors,
                Elevators = [.. currentOptions.Elevators, newElevatorConfig]
            };

            await holder.ResetAsync(newOptions);

            var system = holder.Current;
            var newIndex = system.ElevatorCount - 1;
            var elevatorDto = StatusEndpoints.BuildElevatorDto(system, newIndex);

            return Results.Created($"/api/elevators/{newIndex}", elevatorDto);
        })
        .WithName("AddElevator")
        .Produces<ElevatorDto>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        return group;
    }
}
