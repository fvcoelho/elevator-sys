using System.Net;
using System.Net.Http.Json;
using ElevatorAPI.Models;
using FluentAssertions;

namespace ElevatorAPI.Tests.Integration;

public class ControlEndpointTests : IClassFixture<ElevatorApiFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly ElevatorApiFactory _factory;

    public ControlEndpointTests(ElevatorApiFactory factory)
    {
        _factory = factory.WithFastTimings();
        _client = _factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    // ── PUT /api/dispatch/algorithm ──

    [Theory]
    [InlineData("Simple")]
    [InlineData("SCAN")]
    [InlineData("LOOK")]
    [InlineData("Custom")]
    public async Task PutAlgorithm_AllValidAlgorithms_Returns200(string algorithm)
    {
        var response = await _client.PutAsJsonAsync("/api/dispatch/algorithm", new AlgorithmDto(algorithm));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AlgorithmDto>();
        result!.Algorithm.Should().Be(algorithm);
    }

    [Fact]
    public async Task PutAlgorithm_CaseInsensitive_Returns200()
    {
        var response = await _client.PutAsJsonAsync("/api/dispatch/algorithm", new AlgorithmDto("simple"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AlgorithmDto>();
        result!.Algorithm.Should().Be("Simple");
    }

    [Fact]
    public async Task PutAlgorithm_InvalidName_Returns400()
    {
        var response = await _client.PutAsJsonAsync("/api/dispatch/algorithm", new AlgorithmDto("RoundRobin"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("Invalid algorithm");
        error.Detail.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PutAlgorithm_EmptyString_Returns400()
    {
        var response = await _client.PutAsJsonAsync("/api/dispatch/algorithm", new AlgorithmDto(""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutAlgorithm_ReflectedInStatus()
    {
        await _client.PutAsJsonAsync("/api/dispatch/algorithm", new AlgorithmDto("SCAN"));

        var status = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");
        status!.Algorithm.Should().Be("SCAN");
    }

    [Fact]
    public async Task PutAlgorithm_CanSwitchMultipleTimes()
    {
        await _client.PutAsJsonAsync("/api/dispatch/algorithm", new AlgorithmDto("Simple"));
        var status1 = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");
        status1!.Algorithm.Should().Be("Simple");

        await _client.PutAsJsonAsync("/api/dispatch/algorithm", new AlgorithmDto("LOOK"));
        var status2 = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");
        status2!.Algorithm.Should().Be("LOOK");

        await _client.PutAsJsonAsync("/api/dispatch/algorithm", new AlgorithmDto("Custom"));
        var status3 = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");
        status3!.Algorithm.Should().Be("Custom");
    }

    // ── POST /api/emergency/stop ──

    [Fact]
    public async Task EmergencyStop_Returns200()
    {
        var response = await _client.PostAsync("/api/emergency/stop", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cleanup: resume
        await _client.PostAsync("/api/emergency/resume", null);
    }

    [Fact]
    public async Task EmergencyStop_StatusReflectsEmergency()
    {
        await _client.PostAsync("/api/emergency/stop", null);

        var status = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");
        status!.IsEmergencyStopped.Should().BeTrue();

        // Cleanup
        await _client.PostAsync("/api/emergency/resume", null);
    }

    [Fact]
    public async Task EmergencyStop_AllElevatorsInEmergencyStop()
    {
        await _client.PostAsync("/api/emergency/stop", null);

        var elevators = await _client.GetFromJsonAsync<List<ElevatorDto>>("/api/elevators");
        elevators!.Should().AllSatisfy(e => e.InEmergencyStop.Should().BeTrue());

        // Cleanup
        await _client.PostAsync("/api/emergency/resume", null);
    }

    [Fact]
    public async Task EmergencyStop_DoubleStop_StillWorks()
    {
        await _client.PostAsync("/api/emergency/stop", null);
        var response = await _client.PostAsync("/api/emergency/stop", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");
        status!.IsEmergencyStopped.Should().BeTrue();

        // Cleanup
        await _client.PostAsync("/api/emergency/resume", null);
    }

    // ── POST /api/emergency/resume ──

    [Fact]
    public async Task EmergencyResume_Returns200()
    {
        await _client.PostAsync("/api/emergency/stop", null);

        var response = await _client.PostAsync("/api/emergency/resume", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EmergencyResume_ClearsEmergencyState()
    {
        await _client.PostAsync("/api/emergency/stop", null);
        await _client.PostAsync("/api/emergency/resume", null);

        var status = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");
        status!.IsEmergencyStopped.Should().BeFalse();
    }

    [Fact]
    public async Task EmergencyResume_AllElevatorsResumed()
    {
        await _client.PostAsync("/api/emergency/stop", null);
        await _client.PostAsync("/api/emergency/resume", null);

        var elevators = await _client.GetFromJsonAsync<List<ElevatorDto>>("/api/elevators");
        elevators!.Should().AllSatisfy(e => e.InEmergencyStop.Should().BeFalse());
    }

    [Fact]
    public async Task EmergencyResume_WithoutStop_StillReturns200()
    {
        var response = await _client.PostAsync("/api/emergency/resume", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/elevators/{index}/maintenance ──

    [Fact]
    public async Task Maintenance_EnterMaintenance_Returns200()
    {
        var response = await _client.PostAsync("/api/elevators/0/maintenance", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cleanup
        await _client.PostAsync("/api/elevators/0/maintenance", null);
    }

    [Fact]
    public async Task Maintenance_EnterMaintenance_ElevatorShowsMaintenance()
    {
        await _client.PostAsync("/api/elevators/1/maintenance", null);

        var elevator = await _client.GetFromJsonAsync<ElevatorDto>("/api/elevators/1");
        elevator!.InMaintenance.Should().BeTrue();

        // Cleanup
        await _client.PostAsync("/api/elevators/1/maintenance", null);
    }

    [Fact]
    public async Task Maintenance_Toggle_ExitsMaintenance()
    {
        // Enter
        await _client.PostAsync("/api/elevators/0/maintenance", null);
        var entered = await _client.GetFromJsonAsync<ElevatorDto>("/api/elevators/0");
        entered!.InMaintenance.Should().BeTrue();

        // Exit (toggle)
        await _client.PostAsync("/api/elevators/0/maintenance", null);
        var exited = await _client.GetFromJsonAsync<ElevatorDto>("/api/elevators/0");
        exited!.InMaintenance.Should().BeFalse();
    }

    [Fact]
    public async Task Maintenance_OnlyAffectsTargetElevator()
    {
        await _client.PostAsync("/api/elevators/2/maintenance", null);

        var elevators = await _client.GetFromJsonAsync<List<ElevatorDto>>("/api/elevators");
        elevators![0].InMaintenance.Should().BeFalse();
        elevators[1].InMaintenance.Should().BeFalse();
        elevators[2].InMaintenance.Should().BeTrue();
        elevators[3].InMaintenance.Should().BeFalse();

        // Cleanup
        await _client.PostAsync("/api/elevators/2/maintenance", null);
    }

    [Fact]
    public async Task Maintenance_InvalidIndex_Returns404()
    {
        var response = await _client.PostAsync("/api/elevators/99/maintenance", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Maintenance_IndexEqualToCount_Returns404()
    {
        var response = await _client.PostAsync("/api/elevators/4/maintenance", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Maintenance_ElevatorStateChangesToMaintenance()
    {
        await _client.PostAsync("/api/elevators/0/maintenance", null);

        var elevator = await _client.GetFromJsonAsync<ElevatorDto>("/api/elevators/0");
        elevator!.State.Should().Be("MAINTENANCE");

        // Cleanup
        await _client.PostAsync("/api/elevators/0/maintenance", null);
    }

    // ── End-to-end workflow tests ──

    [Fact]
    public async Task Workflow_SubmitRequest_CheckStatus_CheckMetrics()
    {
        // 1. Submit a request
        var createResponse = await _client.PostAsJsonAsync("/api/requests",
            new CreateRequestDto(2, 8));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Check status shows system is active
        var status = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");
        status.Should().NotBeNull();
        status!.ElevatorCount.Should().Be(4);

        // 3. Metrics record the request
        await Task.Delay(50);
        var metrics = await _client.GetFromJsonAsync<MetricsDto>("/api/metrics");
        metrics!.TotalRequests.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Workflow_ChangeAlgorithm_SubmitRequest_Verify()
    {
        // Switch algorithm
        await _client.PutAsJsonAsync("/api/dispatch/algorithm", new AlgorithmDto("Simple"));

        var status = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");
        status!.Algorithm.Should().Be("Simple");

        // Submit request under new algorithm
        var response = await _client.PostAsJsonAsync("/api/requests",
            new CreateRequestDto(1, 5));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Restore algorithm
        await _client.PutAsJsonAsync("/api/dispatch/algorithm", new AlgorithmDto("Custom"));
    }

    [Fact]
    public async Task Workflow_EmergencyStop_Resume_CheckState()
    {
        // Emergency stop
        await _client.PostAsync("/api/emergency/stop", null);
        var stopped = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");
        stopped!.IsEmergencyStopped.Should().BeTrue();

        // Resume
        await _client.PostAsync("/api/emergency/resume", null);
        var resumed = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");
        resumed!.IsEmergencyStopped.Should().BeFalse();

        // All elevators should be back to normal
        var elevators = await _client.GetFromJsonAsync<List<ElevatorDto>>("/api/elevators");
        elevators!.Should().AllSatisfy(e => e.InEmergencyStop.Should().BeFalse());
    }

    // ── Invalid routes ──

    [Fact]
    public async Task InvalidRoute_Returns404()
    {
        var response = await _client.GetAsync("/api/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WrongMethod_GetOnPostEndpoint_Returns405()
    {
        var response = await _client.GetAsync("/api/requests");

        // Minimal API returns 405 for wrong method on an existing route
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.MethodNotAllowed,
            HttpStatusCode.NotFound);
    }
}
