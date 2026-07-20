using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PTKD.ApiTests;

public class HealthCheckTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(SafeTestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsJson_WithStatusField()
    {
        var response = await _client.GetAsync("/api/v2/health");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK
            || response.StatusCode == HttpStatusCode.ServiceUnavailable);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task HealthEndpoint_Returns_CorrelationId()
    {
        var response = await _client.GetAsync("/api/v2/health");

        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task Response_Contains_CorrelationId_Header()
    {
        var response = await _client.GetAsync("/api/v2/health");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK
            || response.StatusCode == HttpStatusCode.ServiceUnavailable);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
        Assert.False(string.IsNullOrEmpty(correlationId));
    }

    [Fact]
    public async Task Response_Echoes_ClientProvided_CorrelationId()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/health");
        var expectedId = "test-correlation-123";
        request.Headers.Add("X-Correlation-ID", expectedId);

        var response = await _client.SendAsync(request);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK
            || response.StatusCode == HttpStatusCode.ServiceUnavailable);
        var returnedId = response.Headers.GetValues("X-Correlation-ID").First();
        Assert.Equal(expectedId, returnedId);
    }
}
