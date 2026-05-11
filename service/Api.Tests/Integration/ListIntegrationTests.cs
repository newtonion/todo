using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

        var searchResponse = await client.PostAsJsonAsync("/api/list/search", new
        {
            Text = "Weekly"
        });

        searchResponse.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(searchResponse);
        Assert.Equal(1, GetInt32(json.RootElement, "totalCount"));
        var item = Assert.Single(GetProperty(json.RootElement, "items").EnumerateArray());
        Assert.Equal(listId, GetGuid(item, "id"));
        Assert.Equal("Weekly plan", GetString(item, "name"));
        Assert.Equal("Global", GetString(item, "categoryName"));
    }

    [Fact]
    public async Task CreateListWithoutCategory_ReturnsCategorylessList()
    {
        using var factory = new TodoApiFactory();
        using var client = factory.CreateApiClient();

        var createResponse = await client.PostAsJsonAsync("/api/list", new
        {
            Name = "Inbox"
        });
        createResponse.EnsureSuccessStatusCode();
        var listId = await ReadIdAsync(createResponse);

        var getResponse = await client.GetAsync($"/api/list/{listId}");

        getResponse.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(getResponse);
        Assert.Equal("Inbox", GetString(json.RootElement, "name"));
        Assert.Equal(string.Empty, GetString(json.RootElement, "category"));
        Assert.Equal(JsonValueKind.Null, GetProperty(json.RootElement, "categoryId").ValueKind);
    }

    [Fact]
    public async Task CreateListWithoutCategoryGetCountsRenameToggleAndSetCategory()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            var category = AddCategory(context, "Work", TodoApiFactory.TestUserId);
            var list = AddList(context, "Sprint", TodoApiFactory.TestUserId, category.Id);
            AddItem(context, "Done", list.Id, TodoApiFactory.TestUserId, isCompleted: true);
            AddItem(context, "Todo", list.Id, TodoApiFactory.TestUserId);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var listId = await context.Lists.Select(l => l.Id).SingleAsync();
        var categoryId = await context.Categories.Select(c => c.Id).SingleAsync();

        var getResponse = await client.GetAsync($"/api/list/{listId}");
        getResponse.EnsureSuccessStatusCode();
        using var getJson = await ReadJsonAsync(getResponse);
        Assert.Equal("Sprint", GetString(getJson.RootElement, "name"));
        Assert.Equal("Work", GetString(getJson.RootElement, "category"));
        Assert.Equal(categoryId, GetGuid(getJson.RootElement, "categoryId"));
        Assert.False(GetBoolean(getJson.RootElement, "isCompleted"));

        var countsResponse = await client.GetAsync($"/api/list/{listId}/counts");
        countsResponse.EnsureSuccessStatusCode();
        using var countsJson = await ReadJsonAsync(countsResponse);
        Assert.Equal(2, GetInt32(countsJson.RootElement, "totalItems"));
        Assert.Equal(1, GetInt32(countsJson.RootElement, "completedItems"));

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/list/{listId}", new { Name = "Renamed sprint" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/list/{listId}/archive", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/list/{listId}/complete", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"/api/list/{listId}/category", new { Category = (Guid?)null })).StatusCode);

        getResponse = await client.GetAsync($"/api/list/{listId}");
        getResponse.EnsureSuccessStatusCode();
        using var updatedJson = await ReadJsonAsync(getResponse);
        Assert.Equal("Renamed sprint", GetString(updatedJson.RootElement, "name"));
        Assert.Equal(string.Empty, GetString(updatedJson.RootElement, "category"));
        Assert.True(GetBoolean(updatedJson.RootElement, "archived"));
        Assert.True(GetBoolean(updatedJson.RootElement, "isCompleted"));
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

        var defaultResponse = await client.PostAsJsonAsync("/api/list/search", new
        {
            Text = "project"
        });
        defaultResponse.EnsureSuccessStatusCode();
        using var defaultJson = await ReadJsonAsync(defaultResponse);
        Assert.Equal(1, GetInt32(defaultJson.RootElement, "totalCount"));
        Assert.Equal("Open project", GetString(Assert.Single(GetProperty(defaultJson.RootElement, "items").EnumerateArray()), "name"));

        var includeArchivedResponse = await client.PostAsJsonAsync("/api/list/search", new
        {
            Text = "project",
            IncludeArchived = true
        });
        includeArchivedResponse.EnsureSuccessStatusCode();
        using var includeArchivedJson = await ReadJsonAsync(includeArchivedResponse);
        Assert.Equal(2, GetInt32(includeArchivedJson.RootElement, "totalCount"));
    }

    [Fact]
    public async Task Search_FiltersByCategoryTextFromPostBody()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            var workCategory = AddCategory(context, "Work", TodoApiFactory.TestUserId);
            var homeCategory = AddCategory(context, "Home", TodoApiFactory.TestUserId);
            AddList(context, "Sprint", TodoApiFactory.TestUserId, workCategory.Id);
            AddList(context, "Errands", TodoApiFactory.TestUserId, homeCategory.Id);
            AddList(context, "Inbox", TodoApiFactory.TestUserId, null);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/list/search", new
        {
            CategoryText = "wor"
        });

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        Assert.Equal(1, GetInt32(json.RootElement, "totalCount"));
        var item = Assert.Single(GetProperty(json.RootElement, "items").EnumerateArray());
        Assert.Equal("Sprint", GetString(item, "name"));
        Assert.Equal("Work", GetString(item, "categoryName"));
    }

    [Fact]
    public async Task Search_DefaultExcludesCompletedAndIncludeCompletedReturnsThem()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            var category = AddCategory(context, "Work", TodoApiFactory.TestUserId);
            AddList(context, "Open project", TodoApiFactory.TestUserId, category.Id);
            AddList(context, "Completed project", TodoApiFactory.TestUserId, category.Id, isCompleted: true);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();

        var defaultResponse = await client.PostAsJsonAsync("/api/list/search", new
        {
            Text = "project"
        });
        defaultResponse.EnsureSuccessStatusCode();
        using var defaultJson = await ReadJsonAsync(defaultResponse);
        Assert.Equal(1, GetInt32(defaultJson.RootElement, "totalCount"));
        Assert.Equal("Open project", GetString(Assert.Single(GetProperty(defaultJson.RootElement, "items").EnumerateArray()), "name"));

        var includeCompletedResponse = await client.PostAsJsonAsync("/api/list/search", new
        {
            Text = "project",
            IncludeCompleted = true
        });
        includeCompletedResponse.EnsureSuccessStatusCode();
        using var includeCompletedJson = await ReadJsonAsync(includeCompletedResponse);
        Assert.Equal(2, GetInt32(includeCompletedJson.RootElement, "totalCount"));
        Assert.Contains(
            GetProperty(includeCompletedJson.RootElement, "items").EnumerateArray(),
            item => GetString(item, "name") == "Completed project" && GetBoolean(item, "isCompleted"));
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
    public async Task SetCategory_WhenListOrCategoryIsNotAccessible_ReturnsNotFound()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            AddUser(context, "other-user", "Other User", OtherUserId);
            var category = AddCategory(context, "Work", TodoApiFactory.TestUserId);
            var otherCategory = AddCategory(context, "Other category", OtherUserId);
            AddList(context, "Mine", TodoApiFactory.TestUserId, category.Id);
            AddList(context, "Other list", OtherUserId, category.Id);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var ownedListId = await context.Lists.Where(l => l.Name == "Mine").Select(l => l.Id).SingleAsync();
        var otherListId = await context.Lists.Where(l => l.Name == "Other list").Select(l => l.Id).SingleAsync();
        var otherCategoryId = await context.Categories.Where(c => c.Name == "Other category").Select(c => c.Id).SingleAsync();

        var inaccessibleCategoryResponse = await client.PostAsJsonAsync($"/api/list/{ownedListId}/category", new
        {
            Category = otherCategoryId
        });
        var inaccessibleListResponse = await client.PostAsJsonAsync($"/api/list/{otherListId}/category", new
        {
            Category = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.NotFound, inaccessibleCategoryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inaccessibleListResponse.StatusCode);
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
