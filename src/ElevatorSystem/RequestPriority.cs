namespace ElevatorSystem;

/// <summary>
/// Priority levels for elevator requests.
/// Higher values indicate higher priority.
/// </summary>
public enum RequestPriority
{
    /// <summary>
    /// Normal priority - standard passenger request
    /// </summary>
    Normal = 0,

    /// <summary>
    /// High priority - VIP or time-sensitive request requiring immediate attention
    /// </summary>
    High = 1
}
