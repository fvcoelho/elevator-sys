using ElevatorAPI.Configuration;
using ElevatorSystem;

namespace ElevatorAPI.Services;

public static class ElevatorSystemFactory
{
    public static ElevatorSystem.ElevatorSystem Create(ElevatorSystemOptions options)
    {
        var configs = options.Elevators.Select(e => new ElevatorConfig
        {
            Label = e.Label,
            InitialFloor = e.InitialFloor,
            Type = Enum.Parse<ElevatorType>(e.Type, ignoreCase: true),
            Capacity = e.Capacity,
            ServedFloors = e.ServedFloors?.ToHashSet()
        }).ToArray();

        var system = new ElevatorSystem.ElevatorSystem(
            minFloor: options.MinFloor,
            maxFloor: options.MaxFloor,
            doorOpenMs: options.DoorOpenMs,
            floorTravelMs: options.FloorTravelMs,
            doorTransitionMs: options.DoorTransitionMs,
            elevatorConfigs: configs);

        if (Enum.TryParse<DispatchAlgorithm>(options.Algorithm, ignoreCase: true, out var algorithm))
        {
            system.Algorithm = algorithm;
        }

        foreach (var floor in options.VIPFloors)
        {
            system.SetFloorRestriction(floor, FloorRestriction.VIPOnly(floor));
        }

        return system;
    }
}
