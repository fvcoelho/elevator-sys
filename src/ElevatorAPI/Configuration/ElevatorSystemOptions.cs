namespace ElevatorAPI.Configuration;

public class ElevatorSystemOptions
{
    public const string SectionName = "ElevatorSystem";

    public int MinFloor { get; set; } = 1;
    public int MaxFloor { get; set; } = 20;
    public int DoorOpenMs { get; set; } = 3000;
    public int FloorTravelMs { get; set; } = 1500;
    public int DoorTransitionMs { get; set; } = 1000;
    public string Algorithm { get; set; } = "Custom";
    public int[] VIPFloors { get; set; } = [];
    public ElevatorConfigOptions[] Elevators { get; set; } = [];
}
