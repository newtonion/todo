using System.Net.Http.Json;
using static Api.Tests.TestSupport.TestEntities;
using Xunit;

namespace Api.Tests.Integration;

public class CategoryIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Search_ReturnsGlobalAndOwnedCategoriesButExcludesOtherUsers()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            AddUser(context, "other-user", "Other User", OtherUserId);
            AddCategory(context, "Global work", null);
            AddCategory(context, "Owned work", TodoApiFactory.TestUserId);
            AddCategory(context, "Other work", OtherUserId);
            AddCategory(context, "No match", TodoApiFactory.TestUserId);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/category/search", new
        {
            Text = "work"
        });

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        Assert.Equal(2, GetInt32(json.RootElement, "totalCount"));
        var names = GetProperty(json.RootElement, "items")
            .EnumerateArray()
            .Select(item => GetString(item, "name"))
            .Order()
            .ToList();
        Assert.Equal(["Global work", "Owned work"], names);
    }
}
