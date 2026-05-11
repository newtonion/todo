using Api.Domain.Exceptions;
using Api.Infrastructure.Entities;
using Api.Models.Requests;
using Api.Services;
using Api.Tests.TestSupport;
using Api.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Api.Tests.TestSupport.TestEntities;
using Xunit;

namespace Api.Tests.Services;

public class ListServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task CreateAsync_PersistsListOwnedByUserInAccessibleCategory()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var validator = new Mock<IEntityValidator<ListEntity>>();
        await context.SaveChangesAsync();
        var service = CreateService(database, validator.Object);

        var listId = await service.CreateAsync(UserId, "Sprint tasks", category.Id);

        var list = await context.Lists.SingleAsync(l => l.Id == listId);
        Assert.Equal(UserId, list.OwnerId);
        Assert.Equal(category.Id, list.CategoryId);
        Assert.Equal("Sprint tasks", list.Name);
        Assert.False(list.Archived);
        Assert.NotEqual(default, list.CreatedOn);
        Assert.NotEqual(default, list.UpdatedOn);
        validator.Verify(v => v.ValidateAsync(It.Is<ListEntity>(l =>
            l.Id == listId &&
            l.OwnerId == UserId &&
            l.CategoryId == category.Id &&
            l.Name == "Sprint tasks")), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AllowsGlobalCategory()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Shared", null);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var listId = await service.CreateAsync(UserId, "Inbox", category.Id);

        var list = await context.Lists.SingleAsync(l => l.Id == listId);
        Assert.Equal(category.Id, list.CategoryId);
        Assert.Equal(UserId, list.OwnerId);
    }

    [Fact]
    public async Task CreateAsync_AllowsListWithoutCategory()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var service = CreateService(database);

        var listId = await service.CreateAsync(UserId, "Inbox", null);

        var list = await context.Lists.SingleAsync(l => l.Id == listId);
        Assert.Null(list.CategoryId);
        Assert.Equal(UserId, list.OwnerId);
    }

    [Fact]
    public async Task CreateAsync_WhenCategoryIsNotAccessible_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var otherUserCategory = AddCategory(context, "Other", OtherUserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(UserId, "Blocked", otherUserCategory.Id));

        Assert.Empty(await context.Lists.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenValidationFails_DoesNotPersistList()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        await context.SaveChangesAsync();
        var validator = new Mock<IEntityValidator<ListEntity>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ListEntity>()))
            .ThrowsAsync(new ValidationException("Invalid list"));
        var service = CreateService(database, validator.Object);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(UserId, " ", category.Id));

        Assert.Empty(await context.Lists.ToListAsync());
    }

    [Fact]
    public async Task RenameAsync_RenamesOnlyListOwnedByUser()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var ownedList = AddList(context, "Old name", UserId, category.Id);
        var otherUserList = AddList(context, "Other", OtherUserId, category.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await service.RenameAsync(UserId, ownedList.Id, "New name");

        context.ChangeTracker.Clear();
        Assert.Equal("New name", (await context.Lists.FindAsync(ownedList.Id))!.Name);
        Assert.Equal("Other", (await context.Lists.FindAsync(otherUserList.Id))!.Name);
    }

    [Fact]
    public async Task RenameAsync_WhenListIsNotOwnedByUser_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var otherUserList = AddList(context, "Other", OtherUserId, category.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.RenameAsync(UserId, otherUserList.Id, "New name"));
    }

    [Fact]
    public async Task ToggleCompleteAsync_TogglesOwnedListCompletion()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id, isCompleted: false);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await service.ToggleCompleteAsync(UserId, list.Id);

        context.ChangeTracker.Clear();
        Assert.True((await context.Lists.FindAsync(list.Id))!.IsCompleted);

        await service.ToggleCompleteAsync(UserId, list.Id);

        context.ChangeTracker.Clear();
        Assert.False((await context.Lists.FindAsync(list.Id))!.IsCompleted);
    }

    [Fact]
    public async Task ToggleArchiveAsync_TogglesOwnedListArchiveStatus()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id, archived: false);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await service.ToggleArchiveAsync(UserId, list.Id);

        context.ChangeTracker.Clear();
        Assert.True((await context.Lists.FindAsync(list.Id))!.Archived);

        await service.ToggleArchiveAsync(UserId, list.Id);

        context.ChangeTracker.Clear();
        Assert.False((await context.Lists.FindAsync(list.Id))!.Archived);
    }

    [Fact]
    public async Task ToggleCompleteAsyncAndToggleArchiveAsync_WhenListIsNotOwnedByUser_ThrowNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var otherUserList = AddList(context, "Other", OtherUserId, category.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ToggleCompleteAsync(UserId, otherUserList.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.ToggleArchiveAsync(UserId, otherUserList.Id));
    }

    [Fact]
    public async Task SetCategoryAsync_UpdatesOwnedListCategoryToAccessibleCategoryOrNull()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var originalCategory = AddCategory(context, "Work", UserId);
        var globalCategory = AddCategory(context, "Global", null);
        var list = AddList(context, "Sprint", UserId, originalCategory.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await service.SetCategoryAsync(UserId, list.Id, globalCategory.Id);

        context.ChangeTracker.Clear();
        Assert.Equal(globalCategory.Id, (await context.Lists.FindAsync(list.Id))!.CategoryId);

        await service.SetCategoryAsync(UserId, list.Id, null);

        context.ChangeTracker.Clear();
        Assert.Null((await context.Lists.FindAsync(list.Id))!.CategoryId);
    }

    [Fact]
    public async Task SetCategoryAsync_WhenListOrCategoryIsNotAccessible_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var otherUserCategory = AddCategory(context, "Other category", OtherUserId);
        var ownedList = AddList(context, "Mine", UserId, category.Id);
        var otherUserList = AddList(context, "Other list", OtherUserId, category.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.SetCategoryAsync(UserId, ownedList.Id, otherUserCategory.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.SetCategoryAsync(UserId, otherUserList.Id, category.Id));
    }

    [Fact]
    public async Task SearchAsync_ReturnsOwnedNonArchivedListsFilteredSortedPagedWithItemCounts()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var workCategory = AddCategory(context, "Work", UserId);
        var homeCategory = AddCategory(context, "Home", UserId);
        var alpha = AddList(context, "Alpha project", UserId, workCategory.Id, createdOn: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var beta = AddList(context, "Beta project", UserId, homeCategory.Id, createdOn: new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc));
        AddList(context, "Closed project", UserId, workCategory.Id, archived: true);
        AddList(context, "Other project", OtherUserId, workCategory.Id);
        var betaDueDate = new DateTime(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);
        AddItem(context, "Done", alpha.Id, UserId, isCompleted: true, dueDate: new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc));
        AddItem(context, "Todo", alpha.Id, UserId, isCompleted: false, dueDate: new DateTime(2024, 1, 4, 0, 0, 0, DateTimeKind.Utc));
        AddItem(context, "Done too", beta.Id, UserId, isCompleted: true, dueDate: betaDueDate);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.SearchAsync(UserId, new ListSearchCriteria
        {
            Text = "project",
            PageSize = 1,
            Offset = 1,
            OrderBy = new FieldOrderRequest
            {
                Field = "createdOn",
                Ascending = true
            }
        });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(1, result.Offset);
        var item = Assert.Single(result.Items);
        Assert.Equal("Beta project", item.Name);
        Assert.Equal("Home", item.CategoryName);
        Assert.False(item.Archived);
        Assert.Null(item.SoonestDueDate);
        Assert.False(item.IsCompleted);
    }

    [Fact]
    public async Task SearchAsync_WhenIncludeArchivedIsTrue_ReturnsArchivedLists()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        AddList(context, "Open", UserId, category.Id, archived: false);
        AddList(context, "Closed", UserId, category.Id, archived: true);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.SearchAsync(UserId, new ListSearchCriteria
        {
            IncludeArchived = true
        });

        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, l => l.Name == "Open");
        Assert.Contains(result.Items, l => l.Name == "Closed" && l.Archived);
    }

    [Fact]
    public async Task SearchAsync_DefaultExcludesCompletedListsAndIncludeCompletedReturnsThem()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        AddList(context, "Open", UserId, category.Id, isCompleted: false);
        AddList(context, "Completed", UserId, category.Id, isCompleted: true);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var defaultResult = await service.SearchAsync(UserId, new ListSearchCriteria());
        var includeCompletedResult = await service.SearchAsync(UserId, new ListSearchCriteria
        {
            IncludeCompleted = true
        });

        Assert.Equal(["Open"], defaultResult.Items.Select(l => l.Name));
        Assert.Equal(2, includeCompletedResult.TotalCount);
        Assert.Contains(includeCompletedResult.Items, l => l.Name == "Completed" && l.IsCompleted);
    }

    [Fact]
    public async Task SearchAsync_FiltersByCategoryTextAndSupportsListsWithoutCategory()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var workCategory = AddCategory(context, "Work", UserId);
        var homeCategory = AddCategory(context, "Home", UserId);
        AddList(context, "Sprint", UserId, workCategory.Id);
        AddList(context, "Errands", UserId, homeCategory.Id);
        AddList(context, "Inbox", UserId, null);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.SearchAsync(UserId, new ListSearchCriteria
        {
            CategoryText = "wor",
            IncludeCompleted = true
        });
        var allResult = await service.SearchAsync(UserId, new ListSearchCriteria
        {
            IncludeCompleted = true
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("Sprint", item.Name);
        Assert.Equal("Work", item.CategoryName);
        Assert.Contains(allResult.Items, l => l.Name == "Inbox" && l.CategoryName == string.Empty);
    }

    [Fact]
    public async Task GetAsync_ReturnsOwnedListWithCategory()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id, archived: true);
        AddItem(context, "Done", list.Id, UserId, isCompleted: true);
        AddItem(context, "Todo", list.Id, UserId, isCompleted: false);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.GetAsync(UserId, list.Id);

        Assert.Equal(list.Id, result.Id);
        Assert.Equal("Sprint", result.Name);
        Assert.Equal("Work", result.Category);
        Assert.Equal(category.Id, result.CategoryId);
        Assert.True(result.Archived);
        Assert.False(result.Completed);
    }

    [Fact]
    public async Task GetAsync_ReturnsOwnedListWithoutCategory()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var list = AddList(context, "Inbox", UserId, null, isCompleted: true);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.GetAsync(UserId, list.Id);

        Assert.Equal(list.Id, result.Id);
        Assert.Equal("Inbox", result.Name);
        Assert.Equal(string.Empty, result.Category);
        Assert.Null(result.CategoryId);
        Assert.True(result.Completed);
    }

    [Fact]
    public async Task GetAsync_WhenListIsNotOwnedByUser_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var otherUserList = AddList(context, "Other", OtherUserId, category.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(UserId, otherUserList.Id));
    }

    [Fact]
    public async Task GetCountsAsync_ReturnsCountsForOwnedList()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        AddItem(context, "Done", list.Id, UserId, isCompleted: true);
        AddItem(context, "Todo", list.Id, UserId, isCompleted: false);
        AddItem(context, "Other user item", list.Id, OtherUserId, isCompleted: true);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.GetCountsAsync(UserId, list.Id);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(1, result.CompletedItems);
    }

    [Fact]
    public async Task GetCountsAsync_WhenListIsNotOwnedByUser_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var otherUserList = AddList(context, "Other", OtherUserId, category.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetCountsAsync(UserId, otherUserList.Id));
    }

    private static ListService CreateService(
        TestDatabase database,
        IEntityValidator<ListEntity>? validator = null)
    {
        return new ListService(
            new TestDbContextFactory(database.Options),
            validator ?? new ListEntityValidator(),
            NullLogger<ListService>.Instance);
    }

}
