using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using static Api.Tests.TestSupport.TestEntities;
using Xunit;

namespace Api.Tests.Integration;

public class CategoryIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateGetUpdateAndDeleteCategory()
    {
        using var factory = new TodoApiFactory();
        using var client = factory.CreateApiClient();

        var createResponse = await client.PostAsJsonAsync("/api/category", new { Name = "Errands" });
        createResponse.EnsureSuccessStatusCode();
        var categoryId = await ReadIdAsync(createResponse);

        var getResponse = await client.GetAsync($"/api/category/{categoryId}");
        getResponse.EnsureSuccessStatusCode();
        using var getJson = await ReadJsonAsync(getResponse);
        Assert.Equal("Errands", GetString(getJson.RootElement, "name"));

        var updateResponse = await client.PutAsJsonAsync($"/api/category/{categoryId}", new { Name = "Personal" });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        getResponse = await client.GetAsync($"/api/category/{categoryId}");
        getResponse.EnsureSuccessStatusCode();
        using var updatedJson = await ReadJsonAsync(getResponse);
        Assert.Equal("Personal", GetString(updatedJson.RootElement, "name"));

        var deleteResponse = await client.DeleteAsync($"/api/category/{categoryId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var deletedGetResponse = await client.GetAsync($"/api/category/{categoryId}");
        Assert.Equal(HttpStatusCode.NotFound, deletedGetResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WhenBodyIsInvalid_ReturnsBadRequest()
    {
        using var factory = new TodoApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/category", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

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

        var response = await client.GetAsync("/api/category?text=work&pageSize=10&offset=0");

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

    [Fact]
    public async Task UpdateAndDelete_WhenCategoryIsNotOwnedByUser_ReturnNotFound()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            AddUser(context, "other-user", "Other User", OtherUserId);
            AddCategory(context, "Global", null);
            AddCategory(context, "Other", OtherUserId);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var globalCategoryId = await context.Categories.Where(c => c.Name == "Global").Select(c => c.Id).SingleAsync();
        var otherCategoryId = await context.Categories.Where(c => c.Name == "Other").Select(c => c.Id).SingleAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/category/{globalCategoryId}", new { Name = "New" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/category/{globalCategoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/category/{otherCategoryId}", new { Name = "New" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/category/{otherCategoryId}")).StatusCode);
    }
}
