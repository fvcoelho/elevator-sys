using ElevatorAPI.Models;
using ElevatorSystem;

namespace ElevatorAPI.Endpoints;

public static class RequestEndpoints
{
    public static RouteGroupBuilder MapRequestEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/requests", (CreateRequestDto dto, ElevatorSystem.ElevatorSystem system) =>
        {
            try
            {
                var priority = RequestPriority.Normal;
                if (dto.Priority is not null &&
                    !Enum.TryParse<RequestPriority>(dto.Priority, ignoreCase: true, out priority))
                {
                    return Results.BadRequest(new ErrorResponse("Invalid priority", $"Valid values: {string.Join(", ", Enum.GetNames<RequestPriority>())}"));
                }

                AccessLevel? accessLevel = null;
                if (dto.AccessLevel is not null)
                {
                    accessLevel = dto.AccessLevel.Equals("VIP", StringComparison.OrdinalIgnoreCase)
                        ? AccessLevel.VIP
                        : AccessLevel.Standard;
                }

                ElevatorType? preferredType = null;
                if (dto.PreferredElevatorType is not null)
                {
                    if (!Enum.TryParse<ElevatorType>(dto.PreferredElevatorType, ignoreCase: true, out var parsed))
                    {
                        return Results.BadRequest(new ErrorResponse("Invalid elevator type", $"Valid values: {string.Join(", ", Enum.GetNames<ElevatorType>())}"));
                    }
                    preferredType = parsed;
                }

                var request = new Request(
                    dto.PickupFloor,
                    dto.DestinationFloor,
                    priority,
                    accessLevel,
                    preferredElevatorType: preferredType);

                system.AddRequest(request);

                var response = new RequestResponseDto(
                    request.RequestId,
                    request.PickupFloor,
                    request.DestinationFloor,
                    request.Direction.ToString(),
                    request.Priority.ToString(),
                    request.AccessLevel.Name,
                    request.PreferredElevatorType?.ToString());

                return Results.Created($"/api/requests/{request.RequestId}", response);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new ErrorResponse("Invalid floor", ex.Message));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse("Invalid request", ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new ErrorResponse("Access denied", ex.Message), statusCode: 403);
            }
        })
        .WithName("CreateRequest")
        .Produces<RequestResponseDto>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        return group;
    }
}
