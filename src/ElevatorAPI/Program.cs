using System.Net.WebSockets;
using ElevatorAPI.Configuration;
using ElevatorAPI.Endpoints;
using ElevatorAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind configuration
var options = builder.Configuration
    .GetSection(ElevatorSystemOptions.SectionName)
    .Get<ElevatorSystemOptions>() ?? new ElevatorSystemOptions();

// Build elevator system and wrap in holder
var elevatorSystem = ElevatorSystemFactory.Create(options);
builder.Services.AddSingleton(sp =>
    new ElevatorSystemHolder(elevatorSystem, options, sp.GetRequiredService<ILogger<ElevatorSystemHolder>>()));

// Register system runner (replaces per-elevator and dispatcher workers)
builder.Services.AddHostedService<SystemRunnerService>();

// Register WebSocket broadcast service
builder.Services.AddSingleton<WebSocketBroadcastService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<WebSocketBroadcastService>());

// Add CORS for frontend
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseWebSockets();

// WebSocket endpoint
app.Map("/ws", async (HttpContext context, WebSocketBroadcastService broadcaster) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var ws = await context.WebSockets.AcceptWebSocketAsync();
    var id = Guid.NewGuid().ToString();
    broadcaster.AddConnection(id, ws);

    try
    {
        var buffer = new byte[1024];
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }
    catch (WebSocketException)
    {
        // Client disconnected
    }
    finally
    {
        broadcaster.RemoveConnection(id);
    }
});

// Map all endpoint groups under /api
var api = app.MapGroup("/api");
api.MapRequestEndpoints();
api.MapStatusEndpoints();
api.MapMetricsEndpoints();
api.MapControlEndpoints();
api.MapLogEndpoints();

app.Run();

public partial class Program { }
