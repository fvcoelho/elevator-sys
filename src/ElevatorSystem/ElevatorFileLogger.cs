using Microsoft.Extensions.Logging;

namespace ElevatorSystem;

/// <summary>
/// Custom ILogger implementation for writing elevator logs to individual files.
/// Each elevator gets its own log file with timestamped entries.
/// Thread-safe via StreamWriter.AutoFlush.
/// </summary>
public class ElevatorFileLogger : ILogger
{
    private readonly StreamWriter _writer;
    private readonly string _elevatorLabel;

    public ElevatorFileLogger(StreamWriter writer, string elevatorLabel)
    {
        _writer = writer;
        _elevatorLabel = elevatorLabel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        _writer.WriteLine($"[{timestamp}] [Elevator {_elevatorLabel}] {message}");
    }
}
