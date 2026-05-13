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

    [Fact]
    public async Task Print_EmptyList_ReturnsListWithNoItems()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            AddList(context, "Empty List", TodoApiFactory.TestUserId, null);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var listId = await context.Lists.Select(l => l.Id).SingleAsync();

        var response = await client.GetAsync($"/api/list/{listId}/print");

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        Assert.Equal(listId, GetGuid(json.RootElement, "id"));
        Assert.Equal("Empty List", GetString(json.RootElement, "name"));
        Assert.Empty(GetProperty(json.RootElement, "items").EnumerateArray());
    }

    [Fact]
    public async Task Print_ListWithItems_ReturnsItemsWithoutSubItems()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            var list = AddList(context, "Shopping List", TodoApiFactory.TestUserId, null);
            AddItem(context, "Milk", list.Id, TodoApiFactory.TestUserId);
            AddItem(context, "Bread", list.Id, TodoApiFactory.TestUserId, isCompleted: true);
            AddItem(context, "Eggs", list.Id, TodoApiFactory.TestUserId);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var listId = await context.Lists.Select(l => l.Id).SingleAsync();

        var response = await client.GetAsync($"/api/list/{listId}/print");

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        Assert.Equal(listId, GetGuid(json.RootElement, "id"));
        Assert.Equal("Shopping List", GetString(json.RootElement, "name"));
        
        var items = GetProperty(json.RootElement, "items").EnumerateArray().ToList();
        Assert.Equal(3, items.Count);
        
        Assert.Contains(items, i => GetString(i, "name") == "Milk" && !GetBoolean(i, "isCompleted") && GetProperty(i, "subItems").GetArrayLength() == 0);
        Assert.Contains(items, i => GetString(i, "name") == "Bread" && GetBoolean(i, "isCompleted") && GetProperty(i, "subItems").GetArrayLength() == 0);
        Assert.Contains(items, i => GetString(i, "name") == "Eggs" && !GetBoolean(i, "isCompleted") && GetProperty(i, "subItems").GetArrayLength() == 0);
    }

    [Fact]
    public async Task Print_ListWithItemsAndSubItems_ReturnsNestedStructure()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            var list = AddList(context, "Project Tasks", TodoApiFactory.TestUserId, null);
            var task1 = AddItem(context, "Setup", list.Id, TodoApiFactory.TestUserId);
            var task2 = AddItem(context, "Development", list.Id, TodoApiFactory.TestUserId, isCompleted: true);
            
            // Subtasks for Setup
            AddItem(context, "Install dependencies", list.Id, TodoApiFactory.TestUserId, parentListItemId: task1.Id, isCompleted: true);
            AddItem(context, "Configure environment", list.Id, TodoApiFactory.TestUserId, parentListItemId: task1.Id);
            
            // Subtasks for Development
            AddItem(context, "Write code", list.Id, TodoApiFactory.TestUserId, parentListItemId: task2.Id, isCompleted: true);
            AddItem(context, "Write tests", list.Id, TodoApiFactory.TestUserId, parentListItemId: task2.Id, isCompleted: true);
            
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var listId = await context.Lists.Select(l => l.Id).SingleAsync();

        var response = await client.GetAsync($"/api/list/{listId}/print");

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        Assert.Equal("Project Tasks", GetString(json.RootElement, "name"));
        
        var items = GetProperty(json.RootElement, "items").EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        
        // Verify Setup task and its subtasks
        var setup = items.Single(i => GetString(i, "name") == "Setup");
        Assert.False(GetBoolean(setup, "isCompleted"));
        
        var setupSubItems = GetProperty(setup, "subItems").EnumerateArray().ToList();
        Assert.Equal(2, setupSubItems.Count);
        Assert.Contains(setupSubItems, si => GetString(si, "name") == "Install dependencies" && GetBoolean(si, "isCompleted"));
        Assert.Contains(setupSubItems, si => GetString(si, "name") == "Configure environment" && !GetBoolean(si, "isCompleted"));
        
        // Verify Development task and its subtasks
        var development = items.Single(i => GetString(i, "name") == "Development");
        Assert.True(GetBoolean(development, "isCompleted"));
        
        var devSubItems = GetProperty(development, "subItems").EnumerateArray().ToList();
        Assert.Equal(2, devSubItems.Count);
        Assert.Contains(devSubItems, si => GetString(si, "name") == "Write code" && GetBoolean(si, "isCompleted"));
        Assert.Contains(devSubItems, si => GetString(si, "name") == "Write tests" && GetBoolean(si, "isCompleted"));
    }

    [Fact]
    public async Task Print_NonExistentList_ReturnsNotFound()
    {
        using var factory = new TodoApiFactory();
        using var client = factory.CreateApiClient();
        var nonExistentId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/list/{nonExistentId}/print");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Print_OtherUsersList_ReturnsNotFound()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            AddUser(context, "other-user", "Other User", OtherUserId);
            var list = AddList(context, "Other User's List", OtherUserId, null);
            AddItem(context, "Private item", list.Id, OtherUserId);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var listId = await context.Lists.Select(l => l.Id).SingleAsync();

        var response = await client.GetAsync($"/api/list/{listId}/print");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Print_WithCustomSortOrder_ReturnsItemsInSortIndexOrder()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            var list = AddList(context, "Sorted List", TodoApiFactory.TestUserId, null);
            AddItem(context, "Third", list.Id, TodoApiFactory.TestUserId, sortIndex: 2);
            AddItem(context, "First", list.Id, TodoApiFactory.TestUserId, sortIndex: 0);
            AddItem(context, "Second", list.Id, TodoApiFactory.TestUserId, sortIndex: 1);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var listId = await context.Lists.Select(l => l.Id).SingleAsync();

        var response = await client.GetAsync($"/api/list/{listId}/print?OrderBy.Field=customSort&OrderBy.Ascending=true");

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        var items = GetProperty(json.RootElement, "items").EnumerateArray().ToList();
        
        Assert.Equal(3, items.Count);
        Assert.Equal("First", GetString(items[0], "name"));
        Assert.Equal("Second", GetString(items[1], "name"));
        Assert.Equal("Third", GetString(items[2], "name"));
    }

    [Fact]
    public async Task Print_WithNameSort_ReturnsItemsInAlphabeticalOrder()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            var list = AddList(context, "Names List", TodoApiFactory.TestUserId, null);
            AddItem(context, "Zebra", list.Id, TodoApiFactory.TestUserId);
            AddItem(context, "Apple", list.Id, TodoApiFactory.TestUserId);
            AddItem(context, "Mango", list.Id, TodoApiFactory.TestUserId);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var listId = await context.Lists.Select(l => l.Id).SingleAsync();

        var response = await client.GetAsync($"/api/list/{listId}/print?OrderBy.Field=name&OrderBy.Ascending=true");

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        var items = GetProperty(json.RootElement, "items").EnumerateArray().ToList();
        
        Assert.Equal(3, items.Count);
        Assert.Equal("Apple", GetString(items[0], "name"));
        Assert.Equal("Mango", GetString(items[1], "name"));
        Assert.Equal("Zebra", GetString(items[2], "name"));
    }

    [Fact]
    public async Task Print_WithNameSortDescending_ReturnsItemsInReverseAlphabeticalOrder()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            var list = AddList(context, "Names List", TodoApiFactory.TestUserId, null);
            AddItem(context, "Zebra", list.Id, TodoApiFactory.TestUserId);
            AddItem(context, "Apple", list.Id, TodoApiFactory.TestUserId);
            AddItem(context, "Mango", list.Id, TodoApiFactory.TestUserId);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var listId = await context.Lists.Select(l => l.Id).SingleAsync();

        var response = await client.GetAsync($"/api/list/{listId}/print?OrderBy.Field=name&OrderBy.Ascending=false");

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        var items = GetProperty(json.RootElement, "items").EnumerateArray().ToList();
        
        Assert.Equal(3, items.Count);
        Assert.Equal("Zebra", GetString(items[0], "name"));
        Assert.Equal("Mango", GetString(items[1], "name"));
        Assert.Equal("Apple", GetString(items[2], "name"));
    }

    [Fact]
    public async Task Print_WithCompletedSort_ReturnsSortedByCompletionStatus()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            var list = AddList(context, "Task List", TodoApiFactory.TestUserId, null);
            AddItem(context, "Done Task", list.Id, TodoApiFactory.TestUserId, isCompleted: true);
            AddItem(context, "Pending Task", list.Id, TodoApiFactory.TestUserId, isCompleted: false);
            AddItem(context, "Another Done", list.Id, TodoApiFactory.TestUserId, isCompleted: true);
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var listId = await context.Lists.Select(l => l.Id).SingleAsync();

        var response = await client.GetAsync($"/api/list/{listId}/print?OrderBy.Field=completed&OrderBy.Ascending=true");

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        var items = GetProperty(json.RootElement, "items").EnumerateArray().ToList();
        
        Assert.Equal(3, items.Count);
        // completed=false comes first when ascending
        Assert.False(GetBoolean(items[0], "isCompleted"));
        Assert.True(GetBoolean(items[1], "isCompleted"));
        Assert.True(GetBoolean(items[2], "isCompleted"));
    }

    [Fact]
    public async Task Print_WithSortAndSubItems_OnlySortsParentItems()
    {
        using var factory = new TodoApiFactory();
        await factory.SeedAsync(context =>
        {
            var list = AddList(context, "Sorted with Subtasks", TodoApiFactory.TestUserId, null);
            var task1 = AddItem(context, "Z Task", list.Id, TodoApiFactory.TestUserId);
            var task2 = AddItem(context, "A Task", list.Id, TodoApiFactory.TestUserId);
            
            // Add subtasks in specific order (should NOT be affected by parent sort)
            AddItem(context, "Z Subtask", list.Id, TodoApiFactory.TestUserId, parentListItemId: task2.Id);
            AddItem(context, "A Subtask", list.Id, TodoApiFactory.TestUserId, parentListItemId: task2.Id);
            
            return Task.CompletedTask;
        });
        using var client = factory.CreateApiClient();
        await using var context = await CreateDbContextAsync(factory);
        var listId = await context.Lists.Select(l => l.Id).SingleAsync();

        var response = await client.GetAsync($"/api/list/{listId}/print?OrderBy.Field=name&OrderBy.Ascending=true");

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        var items = GetProperty(json.RootElement, "items").EnumerateArray().ToList();
        
        Assert.Equal(2, items.Count);
        // Parent items should be sorted
        Assert.Equal("A Task", GetString(items[0], "name"));
        Assert.Equal("Z Task", GetString(items[1], "name"));
        
        // Subtasks should be in their original order (not sorted by name)
        var subItems = GetProperty(items[0], "subItems").EnumerateArray().ToList();
        Assert.Equal(2, subItems.Count);
        Assert.Equal("Z Subtask", GetString(subItems[0], "name"));
        Assert.Equal("A Subtask", GetString(subItems[1], "name"));
    }
}

