namespace ElevatorAPI.Services;

public class ElevatorWorkerService : BackgroundService
{
    private readonly ElevatorSystem.ElevatorSystem _elevatorSystem;
    private readonly int _elevatorIndex;
    private readonly ILogger<ElevatorWorkerService> _logger;

    public ElevatorWorkerService(
        ElevatorSystem.ElevatorSystem elevatorSystem,
        int elevatorIndex,
        ILogger<ElevatorWorkerService> logger)
    {
        _elevatorSystem = elevatorSystem;
        _elevatorIndex = elevatorIndex;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Elevator worker service starting for elevator {Index}", _elevatorIndex);
        try
        {
            await _elevatorSystem.ProcessElevatorAsync(_elevatorIndex, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Elevator worker service stopping for elevator {Index}", _elevatorIndex);
        }
    }
}
