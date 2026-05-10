using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using static Api.Tests.TestSupport.TestEntities;
using Xunit;

namespace Api.Tests.Integration;

public class ListItemIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Search_WhenQueryContainsDifferentListId_UsesRouteListId()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            var category = AddCategory(context, "Work", TodoApiFactory.TestUserId);
            var routeList = AddList(context, "Route list", TodoApiFactory.TestUserId, category.Id);
            var queryList = AddList(context, "Query list", TodoApiFactory.TestUserId, category.Id);
            AddItem(context, "Route item", routeList.Id, TodoApiFactory.TestUserId);
            AddItem(context, "Query item", queryList.Id, TodoApiFactory.TestUserId);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var routeListId = await context.Lists.Where(l => l.Name == "Route list").Select(l => l.Id).SingleAsync();
        var queryListId = await context.Lists.Where(l => l.Name == "Query list").Select(l => l.Id).SingleAsync();

        var response = await client.GetAsync($"/api/lists/{routeListId}/items?listId={queryListId}");

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        var items = GetProperty(json.RootElement, "items").EnumerateArray().ToList();
        var item = Assert.Single(items);
        Assert.Equal("Route item", GetString(item, "name"));
    }

    [Fact]
    public async Task CreateGetRenameDueDateReorderToggleAndDeleteItem()
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
        var dueDate = new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc);

        var createResponse = await client.PostAsJsonAsync($"/api/lists/{listId}/items", new { Name = "Write tests", DueDate = dueDate });
        createResponse.EnsureSuccessStatusCode();
        var itemId = await ReadIdAsync(createResponse);

        var getResponse = await client.GetAsync($"/api/lists/{listId}/items/{itemId}");
        getResponse.EnsureSuccessStatusCode();
        using var getJson = await ReadJsonAsync(getResponse);
        Assert.Equal("Write tests", GetString(getJson.RootElement, "name"));
        Assert.Equal("Sprint", GetString(getJson.RootElement, "parentName"));
        Assert.Equal("Work", GetString(getJson.RootElement, "categoryName"));
        Assert.False(GetBoolean(getJson.RootElement, "isCompleted"));

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"/api/lists/{listId}/items/{itemId}/rename", new { Name = "Review tests" })).StatusCode);
        var newDueDate = new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"/api/lists/{listId}/items/{itemId}/due-date", new { DueDate = newDueDate })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"/api/lists/{listId}/items/{itemId}/reorder", new { SortIndex = 7 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/lists/{listId}/items/{itemId}/toggle", null)).StatusCode);

        getResponse = await client.GetAsync($"/api/lists/{listId}/items/{itemId}");
        getResponse.EnsureSuccessStatusCode();
        using var updatedJson = await ReadJsonAsync(getResponse);
        Assert.Equal("Review tests", GetString(updatedJson.RootElement, "name"));
        Assert.Equal(newDueDate, GetDateTime(updatedJson.RootElement, "dueDate"));
        Assert.Equal(7, GetInt32(updatedJson.RootElement, "sortIndex"));
        Assert.True(GetBoolean(updatedJson.RootElement, "isCompleted"));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/lists/{listId}/items/{itemId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/lists/{listId}/items/{itemId}")).StatusCode);
    }

    [Fact]
    public async Task Create_WhenListIsNotOwnedByUser_ReturnsNotFound()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            AddUser(context, "other-user", "Other User", OtherUserId);
            var category = AddCategory(context, "Other category", OtherUserId);
            AddList(context, "Other list", OtherUserId, category.Id);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var listId = await context.Lists.Select(l => l.Id).SingleAsync();

        var response = await client.PostAsJsonAsync($"/api/lists/{listId}/items", new { Name = "Blocked" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenBodyIsInvalid_ReturnsBadRequest()
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

        var response = await client.PostAsJsonAsync($"/api/lists/{listId}/items", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Mutations_WhenItemIsNotOwnedByUser_ReturnNotFound()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            AddUser(context, "other-user", "Other User", OtherUserId);
            var category = AddCategory(context, "Other category", OtherUserId);
            var list = AddList(context, "Other list", OtherUserId, category.Id);
            AddItem(context, "Other item", list.Id, OtherUserId);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var listId = await context.Lists.Select(l => l.Id).SingleAsync();
        var itemId = await context.ListItems.Select(li => li.Id).SingleAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/lists/{listId}/items/{itemId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/lists/{listId}/items/{itemId}/rename", new { Name = "New" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/lists/{listId}/items/{itemId}/due-date", new { DueDate = (DateTime?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/lists/{listId}/items/{itemId}/reorder", new { SortIndex = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/lists/{listId}/items/{itemId}/toggle", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/lists/{listId}/items/{itemId}")).StatusCode);
    }

    [Fact]
    public async Task GlobalSearch_ReturnsOwnedItemsAcrossLists()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            AddUser(context, "other-user", "Other User", OtherUserId);
            var category = AddCategory(context, "Work", TodoApiFactory.TestUserId);
            var firstList = AddList(context, "Sprint", TodoApiFactory.TestUserId, category.Id);
            var secondList = AddList(context, "Inbox", TodoApiFactory.TestUserId, category.Id, archived: true);
            AddItem(context, "Urgent sprint", firstList.Id, TodoApiFactory.TestUserId, sortIndex: 1);
            AddItem(context, "Urgent inbox", secondList.Id, TodoApiFactory.TestUserId, isCompleted: true, sortIndex: 2);
            AddItem(context, "Other urgent", firstList.Id, OtherUserId, sortIndex: 3);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/api/listitems?text=Urgent&orderBy.field=customSort&orderBy.ascending=false");

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        Assert.Equal(2, GetInt32(json.RootElement, "totalCount"));
        var items = GetProperty(json.RootElement, "items").EnumerateArray().ToList();
        Assert.Equal(["Urgent inbox", "Urgent sprint"], items.Select(item => GetString(item, "name")).ToList());
        Assert.True(GetBoolean(items[0], "completed"));
        Assert.True(GetBoolean(items[0], "archived"));
    }
}
