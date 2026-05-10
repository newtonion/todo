using System.Net;
using Api.Middleware;
using Xunit;

namespace Api.Tests.Integration;

public class HealthAndErrorIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var factory = new TodoApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task NotFoundException_ReturnsJsonNotFoundResponseWithCorrelationId()
    {
        using var factory = new TodoApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add(CorrelationMiddleware.HeaderName, "integration-correlation");

        var response = await client.GetAsync($"/api/category/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("integration-correlation", response.Headers.GetValues(CorrelationMiddleware.HeaderName).Single());
        using var json = await ReadJsonAsync(response);
        Assert.Equal(404, GetInt32(json.RootElement, "Status"));
        Assert.Equal("Resource not found", GetString(json.RootElement, "Title"));
        Assert.Equal("integration-correlation", GetString(json.RootElement, "CorrelationId"));
    }
}
