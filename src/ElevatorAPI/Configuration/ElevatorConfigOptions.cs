namespace ElevatorAPI.Configuration;

public class ElevatorConfigOptions
{
    public string Label { get; set; } = "";
    public int InitialFloor { get; set; } = 1;
    public string Type { get; set; } = "Local";
    public int Capacity { get; set; } = 10;
    public int[]? ServedFloors { get; set; }
}
