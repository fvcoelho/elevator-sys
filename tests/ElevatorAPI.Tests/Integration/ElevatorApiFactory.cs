using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ElevatorAPI.Tests.Integration;

/// <summary>
/// Custom factory that provides isolated ElevatorSystem instances per test class
/// by overriding config with fast timings for test speed.
/// </summary>
public class ElevatorApiFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _configOverrides = new();

    public ElevatorApiFactory WithFastTimings()
    {
        _configOverrides["ElevatorSystem:DoorOpenMs"] = "10";
        _configOverrides["ElevatorSystem:FloorTravelMs"] = "10";
        _configOverrides["ElevatorSystem:DoorTransitionMs"] = "5";
        return this;
    }

    public ElevatorApiFactory WithAlgorithm(string algorithm)
    {
        _configOverrides[$"ElevatorSystem:Algorithm"] = algorithm;
        return this;
    }

    public ElevatorApiFactory WithNoVIPFloors()
    {
        _configOverrides["ElevatorSystem:VIPFloors:0"] = null;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            if (_configOverrides.Count > 0)
            {
                config.AddInMemoryCollection(_configOverrides!);
            }
        });

        builder.UseEnvironment("Development");
    }
}
