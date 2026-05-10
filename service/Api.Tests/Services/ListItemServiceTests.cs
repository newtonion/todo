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

public class ListItemServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task CreateAsync_PersistsItemInOwnedList()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var dueDate = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var validator = new Mock<IEntityValidator<ListItemEntity>>();
        await context.SaveChangesAsync();
        var service = CreateService(database, validator.Object);

        var itemId = await service.CreateAsync(UserId, list.Id, "Write tests", dueDate);

        var item = await context.ListItems.SingleAsync(li => li.Id == itemId);
        Assert.Equal(UserId, item.OwnerId);
        Assert.Equal(list.Id, item.ParentId);
        Assert.Equal("Write tests", item.Name);
        Assert.Equal(dueDate, item.DueDate);
        Assert.False(item.IsCompleted);
        Assert.NotEqual(default, item.CreatedOn);
        Assert.NotEqual(default, item.UpdatedOn);
        validator.Verify(v => v.ValidateAsync(It.Is<ListItemEntity>(li =>
            li.Id == itemId &&
            li.OwnerId == UserId &&
            li.ParentId == list.Id &&
            li.Name == "Write tests")), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenListIsNotOwnedByUser_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var otherUserList = AddList(context, "Other", OtherUserId, category.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(UserId, otherUserList.Id, "Blocked", null));

        Assert.Empty(await context.ListItems.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenValidationFails_DoesNotPersistItem()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        await context.SaveChangesAsync();
        var validator = new Mock<IEntityValidator<ListItemEntity>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ListItemEntity>()))
            .ThrowsAsync(new ValidationException("Invalid item"));
        var service = CreateService(database, validator.Object);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(UserId, list.Id, " ", null));

        Assert.Empty(await context.ListItems.ToListAsync());
    }

    [Fact]
    public async Task SearchAsync_ReturnsOwnedItemsFilteredSortedPagedWithParentProjection()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id, archived: true);
        var otherList = AddList(context, "Inbox", UserId, category.Id);
        AddItem(context, "Alpha task", list.Id, UserId, sortIndex: 1);
        var dueDate = new DateTime(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
        AddItem(context, "Beta task", list.Id, UserId, isCompleted: true, dueDate: dueDate, sortIndex: 2);
        AddItem(context, "Gamma task", list.Id, UserId, sortIndex: 3);
        AddItem(context, "Other parent task", otherList.Id, UserId, sortIndex: 4);
        AddItem(context, "Other user task", list.Id, OtherUserId, sortIndex: 5);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.SearchAsync(UserId, new ListItemSearchCriteria
        {
            ListId = list.Id,
            Text = "task",
            PageSize = 1,
            Offset = 1,
            OrderBy = new FieldOrderRequest
            {
                Field = "customSort",
                Ascending = false
            }
        });

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(1, result.Offset);
        var item = Assert.Single(result.Items);
        Assert.Equal("Beta task", item.Name);
        Assert.True(item.Completed);
        Assert.Equal(dueDate, item.DueDate);
    }

    [Fact]
    public async Task GetAsync_ReturnsOwnedItemWithParentAndCategory()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var dueDate = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var item = AddItem(context, "Write tests", list.Id, UserId, isCompleted: true, dueDate: dueDate, sortIndex: 7);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.GetAsync(UserId, item.Id);

        Assert.Equal(item.Id, result.Id);
        Assert.Equal("Write tests", result.Name);
        Assert.True(result.IsCompleted);
        Assert.Equal(dueDate, result.DueDate);
        Assert.Equal(7, result.SortIndex);
        Assert.Equal(list.Id, result.ParentId);
        Assert.Equal("Sprint", result.ParentName);
        Assert.Equal("Work", result.CategoryName);
        Assert.Equal(item.CreatedOn, result.CreatedOn);
        Assert.Equal(item.UpdatedOn, result.UpdatedOn);
    }

    [Fact]
    public async Task GetAsync_WhenItemIsNotOwnedByUser_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var otherUserItem = AddItem(context, "Other", list.Id, OtherUserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(UserId, otherUserItem.Id));
    }

    [Fact]
    public async Task RenameAsync_RenamesOwnedItemAndValidates()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var item = AddItem(context, "Old", list.Id, UserId);
        var validator = new Mock<IEntityValidator<ListItemEntity>>();
        await context.SaveChangesAsync();
        var service = CreateService(database, validator.Object);

        await service.RenameAsync(UserId, item.Id, "New");

        context.ChangeTracker.Clear();
        Assert.Equal("New", (await context.ListItems.FindAsync(item.Id))!.Name);
        validator.Verify(v => v.ValidateAsync(It.Is<ListItemEntity>(li =>
            li.Id == item.Id &&
            li.Name == "New")), Times.Once);
    }

    [Fact]
    public async Task SetDueDateAsync_UpdatesOwnedItemDueDate()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var item = AddItem(context, "Task", list.Id, UserId);
        var dueDate = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await service.SetDueDateAsync(UserId, item.Id, dueDate);

        context.ChangeTracker.Clear();
        Assert.Equal(dueDate, (await context.ListItems.FindAsync(item.Id))!.DueDate);
    }

    [Fact]
    public async Task ToggleCompletionAsync_TogglesOwnedItemCompletion()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var item = AddItem(context, "Task", list.Id, UserId, isCompleted: false);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await service.ToggleCompletionAsync(UserId, item.Id);
        context.ChangeTracker.Clear();
        Assert.True((await context.ListItems.FindAsync(item.Id))!.IsCompleted);

        await service.ToggleCompletionAsync(UserId, item.Id);
        context.ChangeTracker.Clear();
        Assert.False((await context.ListItems.FindAsync(item.Id))!.IsCompleted);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOwnedItem()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var ownedItem = AddItem(context, "Mine", list.Id, UserId);
        var otherUserItem = AddItem(context, "Other", list.Id, OtherUserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await service.DeleteAsync(UserId, ownedItem.Id);

        context.ChangeTracker.Clear();
        Assert.Null(await context.ListItems.FindAsync(ownedItem.Id));
        Assert.NotNull(await context.ListItems.FindAsync(otherUserItem.Id));
    }

    [Fact]
    public async Task Mutations_WhenItemIsNotOwnedByUser_ThrowNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var otherUserItem = AddItem(context, "Other", list.Id, OtherUserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.RenameAsync(UserId, otherUserItem.Id, "New"));
        await Assert.ThrowsAsync<NotFoundException>(() => service.SetDueDateAsync(UserId, otherUserItem.Id, null));
        await Assert.ThrowsAsync<NotFoundException>(() => service.ToggleCompletionAsync(UserId, otherUserItem.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(UserId, otherUserItem.Id));
    }

    private static ListItemService CreateService(
        TestDatabase database,
        IEntityValidator<ListItemEntity>? validator = null)
    {
        return new ListItemService(
            new TestDbContextFactory(database.Options),
            validator ?? new ListItemEntityValidator(),
            NullLogger<ListItemService>.Instance);
    }
}
