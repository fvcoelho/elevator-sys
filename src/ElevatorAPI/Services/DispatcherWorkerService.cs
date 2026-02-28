namespace ElevatorAPI.Services;

public class DispatcherWorkerService : BackgroundService
{
    private readonly ElevatorSystem.ElevatorSystem _elevatorSystem;
    private readonly ILogger<DispatcherWorkerService> _logger;

    public DispatcherWorkerService(
        ElevatorSystem.ElevatorSystem elevatorSystem,
        ILogger<DispatcherWorkerService> logger)
    {
        _elevatorSystem = elevatorSystem;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dispatcher worker service starting");
        try
        {
            await _elevatorSystem.ProcessDispatchLoopAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Dispatcher worker service stopping");
        }
    }
}
