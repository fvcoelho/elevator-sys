using ElevatorAPI.Configuration;
using ElevatorAPI.Endpoints;
using ElevatorAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind configuration
var options = builder.Configuration
    .GetSection(ElevatorSystemOptions.SectionName)
    .Get<ElevatorSystemOptions>() ?? new ElevatorSystemOptions();

// Build and register elevator system as singleton
var elevatorSystem = ElevatorSystemFactory.Create(options);
builder.Services.AddSingleton(elevatorSystem);

// Register dispatcher worker
builder.Services.AddHostedService<DispatcherWorkerService>();

// Register per-elevator workers
for (int i = 0; i < elevatorSystem.ElevatorCount; i++)
{
    var index = i;
    builder.Services.AddSingleton<IHostedService>(sp =>
    {
        var system = sp.GetRequiredService<ElevatorSystem.ElevatorSystem>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        return new ElevatorWorkerService(system, index,
            loggerFactory.CreateLogger<ElevatorWorkerService>());
    });
}

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map all endpoint groups under /api
var api = app.MapGroup("/api");
api.MapRequestEndpoints();
api.MapStatusEndpoints();
api.MapMetricsEndpoints();
api.MapControlEndpoints();

app.Run();

public partial class Program { }
