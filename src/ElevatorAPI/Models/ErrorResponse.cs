namespace ElevatorAPI.Models;

public record ErrorResponse(string Error, string? Detail = null);
