using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using static Api.Tests.TestSupport.TestEntities;
using Xunit;

namespace Api.Tests.Integration;

public class ListIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateListInGlobalCategoryAndSearchIt()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            AddCategory(context, "Global", null);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var categoryId = await context.Categories.Select(c => c.Id).SingleAsync();

        var createResponse = await client.PostAsJsonAsync("/api/list", new
        {
            Name = "Weekly plan",
            CategoryId = categoryId
        });
        createResponse.EnsureSuccessStatusCode();
        var listId = await ReadIdAsync(createResponse);

        var searchResponse = await client.GetAsync("/api/list?text=Weekly");

        searchResponse.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(searchResponse);
        Assert.Equal(1, GetInt32(json.RootElement, "totalCount"));
        var item = Assert.Single(GetProperty(json.RootElement, "items").EnumerateArray());
        Assert.Equal(listId, GetGuid(item, "id"));
        Assert.Equal("Weekly plan", GetString(item, "name"));
        Assert.Equal("Global", GetString(item, "categoryName"));
    }

    [Fact]
    public async Task GetRenameCloseOpenAndDeleteList()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            var category = AddCategory(context, "Work", TodoApiFactory.TestUserId);
            AddList(context, "Sprint", TodoApiFactory.TestUserId, category.Id);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var listId = await context.Lists.Select(l => l.Id).SingleAsync();

        var getResponse = await client.GetAsync($"/api/list/{listId}");
        getResponse.EnsureSuccessStatusCode();
        using var getJson = await ReadJsonAsync(getResponse);
        Assert.Equal("Sprint", GetString(getJson.RootElement, "name"));
        Assert.Equal("Work", GetString(getJson.RootElement, "category"));

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/list/{listId}", new { Name = "Renamed sprint" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/list/{listId}/close", null)).StatusCode);

        getResponse = await client.GetAsync($"/api/list/{listId}");
        getResponse.EnsureSuccessStatusCode();
        using var closedJson = await ReadJsonAsync(getResponse);
        Assert.Equal("Renamed sprint", GetString(closedJson.RootElement, "name"));
        Assert.True(GetBoolean(closedJson.RootElement, "archived"));

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/list/{listId}/open", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/list/{listId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/list/{listId}")).StatusCode);
    }

    [Fact]
    public async Task Search_DefaultExcludesArchivedAndIncludeArchivedReturnsThem()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            var category = AddCategory(context, "Work", TodoApiFactory.TestUserId);
            AddList(context, "Open project", TodoApiFactory.TestUserId, category.Id);
            AddList(context, "Closed project", TodoApiFactory.TestUserId, category.Id, archived: true);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();

        var defaultResponse = await client.GetAsync("/api/list?text=project");
        defaultResponse.EnsureSuccessStatusCode();
        using var defaultJson = await ReadJsonAsync(defaultResponse);
        Assert.Equal(1, GetInt32(defaultJson.RootElement, "totalCount"));
        Assert.Equal("Open project", GetString(Assert.Single(GetProperty(defaultJson.RootElement, "items").EnumerateArray()), "name"));

        var includeArchivedResponse = await client.GetAsync("/api/list?text=project&includeArchived=true");
        includeArchivedResponse.EnsureSuccessStatusCode();
        using var includeArchivedJson = await ReadJsonAsync(includeArchivedResponse);
        Assert.Equal(2, GetInt32(includeArchivedJson.RootElement, "totalCount"));
    }

    [Fact]
    public async Task Create_WhenCategoryIsNotAccessible_ReturnsNotFound()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            AddUser(context, "other-user", "Other User", OtherUserId);
            AddCategory(context, "Other", OtherUserId);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var categoryId = await context.Categories.Select(c => c.Id).SingleAsync();

        var response = await client.PostAsJsonAsync("/api/list", new { Name = "Blocked", CategoryId = categoryId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenBodyIsInvalid_ReturnsBadRequest()
    {
        using var factory = new TodoApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/list", new { CategoryId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
