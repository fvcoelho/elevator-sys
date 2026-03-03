using ElevatorAPI.Models;

namespace ElevatorAPI.Endpoints;

public static class LogEndpoints
{
    public static RouteGroupBuilder MapLogEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/logs/{label}", (string label, IWebHostEnvironment env) =>
        {
            // Sanitize label — only allow alphanumeric + dash/underscore
            if (string.IsNullOrEmpty(label) || !System.Text.RegularExpressions.Regex.IsMatch(label, @"^[A-Za-z0-9_\-]+$"))
                return Results.BadRequest(new ErrorResponse("Invalid label", "Label must be alphanumeric"));

            var logsDir = Path.Combine(env.ContentRootPath, "logs");
            var filePath = Path.Combine(logsDir, $"elevator_{label}.log");

            if (!File.Exists(filePath))
                return Results.NotFound(new ErrorResponse("Not found", $"No log file for elevator '{label}'"));

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return Results.File(stream, "text/plain", $"elevator_{label}.log");
        })
        .WithName("DownloadElevatorLog")
        .Produces(StatusCodes.Status200OK, contentType: "text/plain")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return group;
    }
}
