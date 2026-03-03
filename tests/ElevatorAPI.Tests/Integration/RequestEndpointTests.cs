using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ElevatorAPI.Models;
using FluentAssertions;

namespace ElevatorAPI.Tests.Integration;

public class RequestEndpointTests : IClassFixture<ElevatorApiFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly ElevatorApiFactory _factory;

    public RequestEndpointTests(ElevatorApiFactory factory)
    {
        _factory = factory.WithFastTimings();
        _client = _factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    // ── Valid requests ──

    [Fact]
    public async Task PostRequest_MinimalPayload_Returns201WithCorrectBody()
    {
        var dto = new CreateRequestDto(1, 10);

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/requests/");

        var result = await response.Content.ReadFromJsonAsync<RequestResponseDto>();
        result.Should().NotBeNull();
        result!.RequestId.Should().BePositive();
        result.PickupFloor.Should().Be(1);
        result.DestinationFloor.Should().Be(10);
        result.Direction.Should().Be("UP");
        result.Priority.Should().Be("Normal");
        result.AccessLevel.Should().Be("Standard");
        result.PreferredElevatorType.Should().BeNull();
    }

    [Fact]
    public async Task PostRequest_DownDirection_ReturnsDown()
    {
        var dto = new CreateRequestDto(15, 3);

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RequestResponseDto>();
        result!.Direction.Should().Be("DOWN");
    }

    [Fact]
    public async Task PostRequest_HighPriority_Returns201()
    {
        var dto = new CreateRequestDto(5, 1, Priority: "High");

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RequestResponseDto>();
        result!.Priority.Should().Be("High");
    }

    [Fact]
    public async Task PostRequest_NormalPriorityExplicit_Returns201()
    {
        var dto = new CreateRequestDto(2, 8, Priority: "Normal");

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RequestResponseDto>();
        result!.Priority.Should().Be("Normal");
    }

    [Fact]
    public async Task PostRequest_CaseInsensitivePriority_Returns201()
    {
        var dto = new CreateRequestDto(3, 7, Priority: "high");

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RequestResponseDto>();
        result!.Priority.Should().Be("High");
    }

    [Fact]
    public async Task PostRequest_VIPAccess_AutoElevatesPriority()
    {
        var dto = new CreateRequestDto(1, 13, AccessLevel: "VIP");

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RequestResponseDto>();
        result!.AccessLevel.Should().Be("VIP");
        result.Priority.Should().Be("High");
    }

    [Fact]
    public async Task PostRequest_StandardAccess_KeepsNormalPriority()
    {
        var dto = new CreateRequestDto(2, 5, AccessLevel: "Standard");

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RequestResponseDto>();
        result!.AccessLevel.Should().Be("Standard");
        result.Priority.Should().Be("Normal");
    }

    [Fact]
    public async Task PostRequest_PreferredFreight_Returns201()
    {
        var dto = new CreateRequestDto(1, 5, PreferredElevatorType: "Freight");

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RequestResponseDto>();
        result!.PreferredElevatorType.Should().Be("Freight");
    }

    [Fact]
    public async Task PostRequest_PreferredExpress_Returns201()
    {
        var dto = new CreateRequestDto(1, 15, PreferredElevatorType: "Express");

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RequestResponseDto>();
        result!.PreferredElevatorType.Should().Be("Express");
    }

    [Fact]
    public async Task PostRequest_CaseInsensitiveElevatorType_Returns201()
    {
        var dto = new CreateRequestDto(2, 6, PreferredElevatorType: "local");

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RequestResponseDto>();
        result!.PreferredElevatorType.Should().Be("Local");
    }

    [Fact]
    public async Task PostRequest_BoundaryFloors_MinToMax_Returns201()
    {
        var dto = new CreateRequestDto(1, 20);

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RequestResponseDto>();
        result!.PickupFloor.Should().Be(1);
        result.DestinationFloor.Should().Be(20);
    }

    [Fact]
    public async Task PostRequest_BoundaryFloors_MaxToMin_Returns201()
    {
        var dto = new CreateRequestDto(20, 1);

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RequestResponseDto>();
        result!.Direction.Should().Be("DOWN");
    }

    [Fact]
    public async Task PostRequest_AdjacentFloors_Returns201()
    {
        var dto = new CreateRequestDto(5, 6);

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostRequest_MultipleRequests_EachGetsUniqueId()
    {
        var ids = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            var dto = new CreateRequestDto(1, 2 + i);
            var response = await _client.PostAsJsonAsync("/api/requests", dto);
            var result = await response.Content.ReadFromJsonAsync<RequestResponseDto>();
            ids.Add(result!.RequestId);
        }

        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task PostRequest_IncrementsPendingOrTotalRequests()
    {
        var metricsBefore = await _client.GetFromJsonAsync<MetricsDto>("/api/metrics");
        var totalBefore = metricsBefore!.TotalRequests;

        await _client.PostAsJsonAsync("/api/requests", new CreateRequestDto(3, 10));

        // Give the system a moment to process
        await Task.Delay(50);

        var metricsAfter = await _client.GetFromJsonAsync<MetricsDto>("/api/metrics");
        metricsAfter!.TotalRequests.Should().BeGreaterThan(totalBefore);
    }

    // ── Invalid requests → 400 ──

    [Fact]
    public async Task PostRequest_PickupFloorBelowMin_Returns400()
    {
        var dto = new CreateRequestDto(0, 10);

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PostRequest_PickupFloorAboveMax_Returns400()
    {
        var dto = new CreateRequestDto(21, 10);

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostRequest_DestinationFloorBelowMin_Returns400()
    {
        var dto = new CreateRequestDto(5, 0);

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostRequest_DestinationFloorAboveMax_Returns400()
    {
        var dto = new CreateRequestDto(1, 99);

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostRequest_NegativeFloor_Returns400()
    {
        var dto = new CreateRequestDto(-1, 5);

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostRequest_SamePickupAndDestination_Returns400()
    {
        var dto = new CreateRequestDto(5, 5);

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostRequest_InvalidPriority_Returns400()
    {
        var dto = new CreateRequestDto(1, 10, Priority: "Urgent");

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("priority", Exactly.Once(), "should mention priority in error");
    }

    [Fact]
    public async Task PostRequest_InvalidElevatorType_Returns400()
    {
        var dto = new CreateRequestDto(1, 10, PreferredElevatorType: "Teleporter");

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("elevator type", Exactly.Once(), "should mention elevator type in error");
    }

    [Fact]
    public async Task PostRequest_EmptyBody_Returns400()
    {
        var response = await _client.PostAsync("/api/requests",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        // Empty body with defaults of 0,0 should fail validation
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Access control → 403 ──

    [Fact]
    public async Task PostRequest_StandardUserToVIPFloor_Returns403()
    {
        // Floor 13 is VIP-only per appsettings
        var dto = new CreateRequestDto(1, 13);

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task PostRequest_StandardUserFromVIPFloor_Returns403()
    {
        // Pickup from VIP floor should also be denied
        var dto = new CreateRequestDto(13, 1);

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostRequest_VIPUserToVIPFloor_Returns201()
    {
        var dto = new CreateRequestDto(1, 13, AccessLevel: "VIP");

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── Content-Type handling ──

    [Fact]
    public async Task PostRequest_ResponseIsJson()
    {
        var dto = new CreateRequestDto(2, 8);

        var response = await _client.PostAsJsonAsync("/api/requests", dto);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }
}
