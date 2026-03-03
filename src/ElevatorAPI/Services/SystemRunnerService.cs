namespace ElevatorAPI.Services;

public class SystemRunnerService : BackgroundService
{
    private readonly ElevatorSystemHolder _holder;
    private readonly ILogger<SystemRunnerService> _logger;
    private CancellationTokenSource? _internalCts;
    private readonly object _taskLock = new();
    private List<Task> _runningTasks = [];

    public SystemRunnerService(ElevatorSystemHolder holder, ILogger<SystemRunnerService> logger)
    {
        _holder = holder;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _holder.SystemReplaced += OnSystemReplaced;

        try
        {
            SpawnWorkers(_holder.Current, stoppingToken);

            // Keep running until the host shuts down
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("System runner service stopping");
        }
        finally
        {
            _holder.SystemReplaced -= OnSystemReplaced;
            CancelInternalTasks();
        }
    }

    private void OnSystemReplaced(ElevatorSystem.ElevatorSystem newSystem)
    {
        _logger.LogInformation("System replaced, restarting workers for {Count} elevators", newSystem.ElevatorCount);
        CancelInternalTasks();
        SpawnWorkers(newSystem, default);
    }

    private void SpawnWorkers(ElevatorSystem.ElevatorSystem system, CancellationToken hostToken)
    {
        lock (_taskLock)
        {
            _internalCts = hostToken == default
                ? new CancellationTokenSource()
                : CancellationTokenSource.CreateLinkedTokenSource(hostToken);

            var token = _internalCts.Token;
            var tasks = new List<Task>();

            // Dispatch loop
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Dispatch worker starting");
                    await system.ProcessDispatchLoopAsync(token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Dispatch worker failed");
                }
            }, token));

            // Per-elevator workers
            for (int i = 0; i < system.ElevatorCount; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("Elevator worker {Index} starting", index);
                        await system.ProcessElevatorAsync(index, token);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Elevator worker {Index} failed", index);
                    }
                }, token));
            }

            _runningTasks = tasks;
        }
    }

    private void CancelInternalTasks()
    {
        lock (_taskLock)
        {
            if (_internalCts is not null)
            {
                _internalCts.Cancel();
                _internalCts.Dispose();
                _internalCts = null;
            }
            _runningTasks = [];
        }
    }
}
