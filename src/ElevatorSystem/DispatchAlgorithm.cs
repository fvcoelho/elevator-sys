namespace ElevatorSystem;

/// <summary>
/// Dispatch algorithms for assigning requests to elevators
/// </summary>
public enum DispatchAlgorithm
{
    /// <summary>
    /// Simple closest elevator algorithm with idle preference
    /// Fastest but may not be most efficient for high traffic
    /// </summary>
    Simple,

    /// <summary>
    /// SCAN algorithm (elevator continues in current direction until no more requests)
    /// Also known as "Look" in disk scheduling
    /// </summary>
    SCAN,

    /// <summary>
    /// LOOK algorithm (elevator reverses when no more requests in current direction)
    /// More efficient than SCAN for typical elevator usage patterns
    /// </summary>
    LOOK
}
