namespace ElevatorSystem;

/// <summary>
/// Configuration for creating an elevator with specific properties
/// </summary>
public class ElevatorConfig
{
    /// <summary>
    /// Elevator label (e.g., "A", "B", "C")
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Initial floor position for the elevator
    /// </summary>
    public int InitialFloor { get; set; }

    /// <summary>
    /// Type of elevator (Local, Express, Freight)
    /// </summary>
    public ElevatorType Type { get; set; } = ElevatorType.Local;

    /// <summary>
    /// Set of floors this elevator can serve. Null means all floors.
    /// </summary>
    public HashSet<int>? ServedFloors { get; set; } = null;

    /// <summary>
    /// Elevator capacity (weight or passenger count)
    /// </summary>
    public int Capacity { get; set; } = 10;
}
