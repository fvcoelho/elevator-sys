namespace ElevatorSystem;

/// <summary>
/// Defines access levels for users/requests
/// </summary>
public class AccessLevel
{
    /// <summary>
    /// Name of the access level (e.g., "Standard", "VIP", "Executive")
    /// </summary>
    public string Name { get; set; } = "Standard";

    /// <summary>
    /// Set of floors this access level can reach
    /// Null means no restrictions (can access all floors)
    /// </summary>
    public HashSet<int>? AllowedFloors { get; set; } = null;

    /// <summary>
    /// Whether this is a VIP access level (gets priority treatment)
    /// </summary>
    public bool IsVIP { get; set; } = false;

    /// <summary>
    /// Create a standard access level
    /// </summary>
    public static AccessLevel Standard => new()
    {
        Name = "Standard",
        AllowedFloors = null, // All floors
        IsVIP = false
    };

    /// <summary>
    /// Create a VIP access level with no floor restrictions
    /// </summary>
    public static AccessLevel VIP => new()
    {
        Name = "VIP",
        AllowedFloors = null, // All floors
        IsVIP = true
    };

    /// <summary>
    /// Create a custom VIP access level with specific floors
    /// </summary>
    public static AccessLevel CreateVIP(string name, HashSet<int>? allowedFloors = null)
    {
        return new AccessLevel
        {
            Name = name,
            AllowedFloors = allowedFloors,
            IsVIP = true
        };
    }

    /// <summary>
    /// Create a custom access level
    /// </summary>
    public static AccessLevel Create(string name, HashSet<int>? allowedFloors = null, bool isVIP = false)
    {
        return new AccessLevel
        {
            Name = name,
            AllowedFloors = allowedFloors,
            IsVIP = isVIP
        };
    }
}

/// <summary>
/// Defines restrictions for a specific floor
/// </summary>
public class FloorRestriction
{
    /// <summary>
    /// The floor number this restriction applies to
    /// </summary>
    public int Floor { get; set; }

    /// <summary>
    /// Whether this floor requires VIP access
    /// </summary>
    public bool RequiresVIP { get; set; } = false;

    /// <summary>
    /// Specific access level names that are allowed
    /// Empty set means no additional restrictions beyond VIP check
    /// </summary>
    public HashSet<string> AllowedAccessLevels { get; set; } = new();

    /// <summary>
    /// Create a VIP-only floor restriction
    /// </summary>
    public static FloorRestriction VIPOnly(int floor)
    {
        return new FloorRestriction
        {
            Floor = floor,
            RequiresVIP = true,
            AllowedAccessLevels = new HashSet<string>()
        };
    }

    /// <summary>
    /// Create a restriction for specific access levels
    /// </summary>
    public static FloorRestriction ForAccessLevels(int floor, params string[] accessLevels)
    {
        return new FloorRestriction
        {
            Floor = floor,
            RequiresVIP = false,
            AllowedAccessLevels = new HashSet<string>(accessLevels)
        };
    }
}
