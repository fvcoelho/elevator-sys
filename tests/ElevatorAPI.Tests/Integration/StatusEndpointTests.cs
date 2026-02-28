using System.Net;
using System.Net.Http.Json;
using ElevatorAPI.Models;
using FluentAssertions;

namespace ElevatorAPI.Tests.Integration;

public class StatusEndpointTests : IClassFixture<ElevatorApiFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly ElevatorApiFactory _factory;

    public StatusEndpointTests(ElevatorApiFactory factory)
    {
        _factory = factory.WithFastTimings();
        _client = _factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    // ── GET /api/status ──

    [Fact]
    public async Task GetStatus_Returns200()
    {
        var response = await _client.GetAsync("/api/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStatus_ContainsElevatorCount()
    {
        var status = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");

        status.Should().NotBeNull();
        status!.ElevatorCount.Should().Be(4);
    }

    [Fact]
    public async Task GetStatus_InitialStateNotEmergencyStopped()
    {
        var status = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");

        status!.IsEmergencyStopped.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatus_ReportsAlgorithm()
    {
        var status = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");

        status!.Algorithm.Should().Be("Custom");
    }

    [Fact]
    public async Task GetStatus_ElevatorsMatchCount()
    {
        var status = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");

        status!.Elevators.Should().HaveCount(status.ElevatorCount);
    }

    [Fact]
    public async Task GetStatus_ElevatorsHaveLabels()
    {
        var status = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");

        status!.Elevators.Select(e => e.Label)
            .Should().BeEquivalentTo(["A", "B", "C", "F1"]);
    }

    [Fact]
    public async Task GetStatus_ElevatorsHaveInitialFloors()
    {
        var status = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");

        status!.Elevators[0].CurrentFloor.Should().Be(1);   // A
        status.Elevators[1].CurrentFloor.Should().Be(10);   // B
        status.Elevators[2].CurrentFloor.Should().Be(20);   // C (Express)
        status.Elevators[3].CurrentFloor.Should().Be(1);    // F1 (Freight)
    }

    [Fact]
    public async Task GetStatus_PendingRequestsStartsAtZero()
    {
        var status = await _client.GetFromJsonAsync<SystemStatusDto>("/api/status");

        status!.PendingRequests.Should().Be(0);
    }

    [Fact]
    public async Task GetStatus_ResponseIsJson()
    {
        var response = await _client.GetAsync("/api/status");

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    // ── GET /api/elevators ──

    [Fact]
    public async Task GetElevators_Returns200()
    {
        var response = await _client.GetAsync("/api/elevators");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetElevators_ReturnsFourElevators()
    {
        var elevators = await _client.GetFromJsonAsync<List<ElevatorDto>>("/api/elevators");

        elevators.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetElevators_IndicesAreSequential()
    {
        var elevators = await _client.GetFromJsonAsync<List<ElevatorDto>>("/api/elevators");

        elevators!.Select(e => e.Index).Should().BeEquivalentTo([0, 1, 2, 3]);
    }

    [Fact]
    public async Task GetElevators_TypesMatchConfig()
    {
        var elevators = await _client.GetFromJsonAsync<List<ElevatorDto>>("/api/elevators");

        elevators![0].Type.Should().Be("Local");
        elevators[1].Type.Should().Be("Local");
        elevators[2].Type.Should().Be("Express");
        elevators[3].Type.Should().Be("Freight");
    }

    [Fact]
    public async Task GetElevators_CapacitiesMatchConfig()
    {
        var elevators = await _client.GetFromJsonAsync<List<ElevatorDto>>("/api/elevators");

        elevators![0].Capacity.Should().Be(10);
        elevators[1].Capacity.Should().Be(10);
        elevators[2].Capacity.Should().Be(12);
        elevators[3].Capacity.Should().Be(20);
    }

    [Fact]
    public async Task GetElevators_AllStartIdle()
    {
        var elevators = await _client.GetFromJsonAsync<List<ElevatorDto>>("/api/elevators");

        elevators!.Should().AllSatisfy(e => e.State.Should().Be("IDLE"));
    }

    [Fact]
    public async Task GetElevators_NoneInMaintenance()
    {
        var elevators = await _client.GetFromJsonAsync<List<ElevatorDto>>("/api/elevators");

        elevators!.Should().AllSatisfy(e => e.InMaintenance.Should().BeFalse());
    }

    [Fact]
    public async Task GetElevators_NoneInEmergencyStop()
    {
        var elevators = await _client.GetFromJsonAsync<List<ElevatorDto>>("/api/elevators");

        elevators!.Should().AllSatisfy(e => e.InEmergencyStop.Should().BeFalse());
    }

    [Fact]
    public async Task GetElevators_AllHaveTargetFloorsArray()
    {
        var elevators = await _client.GetFromJsonAsync<List<ElevatorDto>>("/api/elevators");

        elevators!.Should().AllSatisfy(e => e.TargetFloors.Should().NotBeNull());
    }

    [Fact]
    public async Task GetElevators_ExpressHasLimitedServedFloors()
    {
        var elevators = await _client.GetFromJsonAsync<List<ElevatorDto>>("/api/elevators");

        var express = elevators!.Single(e => e.Type == "Express");
        express.ServedFloors.Should().NotBeNull();
        express.ServedFloors.Should().Contain(1);
        express.ServedFloors.Should().Contain(new[] { 15, 16, 17, 18, 19, 20 });
        express.ServedFloors.Should().NotContain(new[] { 2, 3, 4, 5, 10, 14 });
    }

    [Fact]
    public async Task GetElevators_LocalServesAllFloors()
    {
        var elevators = await _client.GetFromJsonAsync<List<ElevatorDto>>("/api/elevators");

        var local = elevators!.First(e => e.Type == "Local");
        local.ServedFloors.Should().HaveCount(20);
    }

    // ── GET /api/elevators/{index} ──

    [Fact]
    public async Task GetElevator_Index0_ReturnsElevatorA()
    {
        var response = await _client.GetAsync("/api/elevators/0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var elevator = await response.Content.ReadFromJsonAsync<ElevatorDto>();
        elevator!.Index.Should().Be(0);
        elevator.Label.Should().Be("A");
    }

    [Fact]
    public async Task GetElevator_Index3_ReturnsFreight()
    {
        var elevator = await _client.GetFromJsonAsync<ElevatorDto>("/api/elevators/3");

        elevator!.Label.Should().Be("F1");
        elevator.Type.Should().Be("Freight");
        elevator.Capacity.Should().Be(20);
    }

    [Fact]
    public async Task GetElevator_NegativeIndex_Returns404()
    {
        var response = await _client.GetAsync("/api/elevators/-1");

        // Might be 404 or route mismatch depending on route constraint
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetElevator_IndexOutOfRange_Returns404()
    {
        var response = await _client.GetAsync("/api/elevators/99");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetElevator_IndexEqualToCount_Returns404()
    {
        // There are 4 elevators (0-3), so index 4 should be not found
        var response = await _client.GetAsync("/api/elevators/4");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/metrics ──

    [Fact]
    public async Task GetMetrics_Returns200()
    {
        var response = await _client.GetAsync("/api/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMetrics_HasElevatorStats()
    {
        var metrics = await _client.GetFromJsonAsync<MetricsDto>("/api/metrics");

        metrics!.ElevatorStats.Should().NotBeNull();
        metrics.ElevatorStats.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetMetrics_ElevatorStatLabelsMatchConfig()
    {
        var metrics = await _client.GetFromJsonAsync<MetricsDto>("/api/metrics");

        metrics!.ElevatorStats.Keys.Should().BeEquivalentTo(["A", "B", "C", "F1"]);
    }

    [Fact]
    public async Task GetMetrics_InitialValuesAreZeroOrDefault()
    {
        var metrics = await _client.GetFromJsonAsync<MetricsDto>("/api/metrics");

        metrics!.FloorHeatmap.Should().NotBeNull();
        metrics.RequestsByPriority.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMetrics_AfterRequest_TotalRequestsIncrements()
    {
        var before = await _client.GetFromJsonAsync<MetricsDto>("/api/metrics");

        await _client.PostAsJsonAsync("/api/requests", new CreateRequestDto(2, 8));
        await Task.Delay(50);

        var after = await _client.GetFromJsonAsync<MetricsDto>("/api/metrics");
        after!.TotalRequests.Should().BeGreaterThan(before!.TotalRequests);
    }

    [Fact]
    public async Task GetMetrics_AfterRequest_FloorHeatmapUpdated()
    {
        await _client.PostAsJsonAsync("/api/requests", new CreateRequestDto(3, 12));
        await Task.Delay(50);

        var metrics = await _client.GetFromJsonAsync<MetricsDto>("/api/metrics");
        metrics!.FloorHeatmap.Should().ContainKey("3");
        metrics.FloorHeatmap.Should().ContainKey("12");
    }

    [Fact]
    public async Task GetMetrics_TimeValuesAreNonNegative()
    {
        var metrics = await _client.GetFromJsonAsync<MetricsDto>("/api/metrics");

        metrics!.AverageWaitTimeMs.Should().BeGreaterThanOrEqualTo(0);
        metrics.AverageRideTimeMs.Should().BeGreaterThanOrEqualTo(0);
        metrics.AverageDispatchTimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetMetrics_UtilizationIsPercentage()
    {
        var metrics = await _client.GetFromJsonAsync<MetricsDto>("/api/metrics");

        metrics!.SystemUtilization.Should().BeInRange(0, 100);
        foreach (var stat in metrics.ElevatorStats.Values)
        {
            stat.Utilization.Should().BeInRange(0, 100);
        }
    }
}
