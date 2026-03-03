using ElevatorAPI.Configuration;

namespace ElevatorAPI.Services;

public class ElevatorSystemHolder
{
    private volatile ElevatorSystem.ElevatorSystem _current;
    private readonly SemaphoreSlim _resetLock = new(1, 1);
    private readonly ILogger<ElevatorSystemHolder> _logger;

    public ElevatorSystemHolder(ElevatorSystem.ElevatorSystem initial, ElevatorSystemOptions initialOptions, ILogger<ElevatorSystemHolder> logger)
    {
        _current = initial;
        CurrentOptions = initialOptions;
        _logger = logger;
    }

    public ElevatorSystem.ElevatorSystem Current => _current;

    public ElevatorSystemOptions CurrentOptions { get; private set; }

    public event Action<ElevatorSystem.ElevatorSystem>? SystemReplaced;

    public async Task ResetAsync(ElevatorSystemOptions options)
    {
        await _resetLock.WaitAsync();
        try
        {
            _logger.LogInformation("Resetting elevator system with {Count} elevators, floors {Min}-{Max}",
                options.Elevators.Length, options.MinFloor, options.MaxFloor);

            var newSystem = ElevatorSystemFactory.Create(options);
            var oldSystem = _current;
            CurrentOptions = options;
            _current = newSystem;

            SystemReplaced?.Invoke(newSystem);

            _logger.LogInformation("Elevator system reset complete");
        }
        finally
        {
            _resetLock.Release();
        }
    }
}
