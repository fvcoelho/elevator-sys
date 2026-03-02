using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ElevatorAPI.Endpoints;
using ElevatorAPI.Models;

namespace ElevatorAPI.Services;

public class WebSocketBroadcastService : BackgroundService
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    private readonly ElevatorSystemHolder _holder;
    private readonly ILogger<WebSocketBroadcastService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public WebSocketBroadcastService(
        ElevatorSystemHolder holder,
        ILogger<WebSocketBroadcastService> logger)
    {
        _holder = holder;
        _logger = logger;
    }

    public void AddConnection(string id, WebSocket webSocket)
    {
        _connections.TryAdd(id, webSocket);
        _logger.LogInformation("WebSocket client connected: {Id}. Total: {Count}", id, _connections.Count);
    }

    public void RemoveConnection(string id)
    {
        _connections.TryRemove(id, out _);
        _logger.LogInformation("WebSocket client disconnected: {Id}. Total: {Count}", id, _connections.Count);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebSocket broadcast service starting");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_connections.IsEmpty)
                {
                    await BroadcastStatusAsync(stoppingToken);
                }

                await Task.Delay(500, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("WebSocket broadcast service stopping");
        }
    }

    private async Task BroadcastStatusAsync(CancellationToken cancellationToken)
    {
        var system = _holder.Current;
        var elevators = StatusEndpoints.BuildElevatorDtos(system);
        var status = new SystemStatusDto(
            system.ElevatorCount,
            system.PendingRequestCount,
            system.IsEmergencyStopped,
            system.Algorithm.ToString(),
            system.PeopleWaiting,
            system.PeopleInTransit,
            Process.GetCurrentProcess().WorkingSet64,
            elevators);

        var json = JsonSerializer.Serialize(status, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        var deadConnections = new List<string>();

        foreach (var (id, ws) in _connections)
        {
            if (ws.State != WebSocketState.Open)
            {
                deadConnections.Add(id);
                continue;
            }

            try
            {
                await ws.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send to WebSocket client {Id}", id);
                deadConnections.Add(id);
            }
        }

        foreach (var id in deadConnections)
        {
            RemoveConnection(id);
        }
    }
}
