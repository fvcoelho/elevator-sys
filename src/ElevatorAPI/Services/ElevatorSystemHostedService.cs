namespace ElevatorAPI.Services;

public class ElevatorSystemHostedService : BackgroundService
{
    private readonly ElevatorSystem.ElevatorSystem _elevatorSystem;
    private readonly ILogger<ElevatorSystemHostedService> _logger;

    public ElevatorSystemHostedService(
        ElevatorSystem.ElevatorSystem elevatorSystem,
        ILogger<ElevatorSystemHostedService> logger)
    {
        _elevatorSystem = elevatorSystem;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Elevator system background service starting");
        try
        {
            await _elevatorSystem.ProcessRequestsAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Elevator system background service stopping");
        }
    }
}
