namespace ElevatorSystem;

/// <summary>
/// Types of elevators with different capabilities
/// </summary>
public enum ElevatorType
{
    /// <summary>
    /// Standard elevator serving all floors
    /// </summary>
    Local,

    /// <summary>
    /// Express elevator serving select floors only (e.g., lobby and upper floors)
    /// </summary>
    Express,

    /// <summary>
    /// Freight elevator with higher capacity and slower speed
    /// </summary>
    Freight
}
