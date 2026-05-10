using System.Net;
using Xunit;

namespace Api.Tests.Integration;

public class UserIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Negotiate_ReturnsOk()
    {
        using var factory = new TodoApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/api/user");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
