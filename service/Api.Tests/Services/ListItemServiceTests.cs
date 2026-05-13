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

        var itemId = await service.CreateAsync(UserId, list.Id, null, "Write tests", dueDate);

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

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(UserId, otherUserList.Id, null, "Blocked", null));

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

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(UserId, list.Id, null, " ", null));

        Assert.Empty(await context.ListItems.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenParentListItemIdProvided_CreatesChildItem()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var parentItem = AddItem(context, "Parent task", list.Id, UserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var childItemId = await service.CreateAsync(UserId, list.Id, parentItem.Id, "Child task", null);

        var childItem = await context.ListItems.SingleAsync(li => li.Id == childItemId);
        Assert.Equal("Child task", childItem.Name);
        Assert.Equal(list.Id, childItem.ParentId);
        Assert.Equal(parentItem.Id, childItem.ParentListItemId);
        Assert.Equal(UserId, childItem.OwnerId);
    }

    [Fact]
    public async Task CreateAsync_WhenParentListItemNotFound_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => 
            service.CreateAsync(UserId, list.Id, Guid.NewGuid(), "Child task", null));

        Assert.Empty(await context.ListItems.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenParentListItemInDifferentList_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list1 = AddList(context, "List 1", UserId, category.Id);
        var list2 = AddList(context, "List 2", UserId, category.Id);
        var parentItemInList1 = AddItem(context, "Parent in list 1", list1.Id, UserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => 
            service.CreateAsync(UserId, list2.Id, parentItemInList1.Id, "Child task", null));
    }

    [Fact]
    public async Task CreateAsync_WhenParentListItemNotOwnedByUser_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var otherUserItem = AddItem(context, "Other user's item", list.Id, OtherUserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => 
            service.CreateAsync(UserId, list.Id, otherUserItem.Id, "Child task", null));
    }

    [Fact]
    public async Task CreateAsync_WhenParentListItemIsChild_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var parentItem = AddItem(context, "Parent task", list.Id, UserId);
        var childItem = AddItem(context, "Child task", list.Id, UserId, parentListItemId: parentItem.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateAsync(UserId, list.Id, childItem.Id, "Nested child task", null));
    }

    [Fact]
    public async Task CreateAsync_WhenParentHasMoreThan10Children_ThrowsValidationException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var parentItem = AddItem(context, "Parent task", list.Id, UserId);
        await context.SaveChangesAsync();
        
        // Add 10 children
        for (int i = 0; i < 10; i++)
        {
            AddItem(context, $"Child {i}", list.Id, UserId, parentListItemId: parentItem.Id);
        }
        await context.SaveChangesAsync();
        var service = CreateService(database);

        // Trying to add 11th child should fail
        await Assert.ThrowsAsync<ValidationException>(() => 
            service.CreateAsync(UserId, list.Id, parentItem.Id, "Child 11", null));
    }

    [Fact]
    public async Task CreateAsync_WhenUnrelatedRowsReferenceParentListItemId_DoesNotCountThemAgainstChildLimit()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var otherList = AddList(context, "Backlog", UserId, category.Id);
        var parentItem = AddItem(context, "Parent task", list.Id, UserId);
        await context.SaveChangesAsync();

        for (int i = 0; i < 10; i++)
        {
            AddItem(context, $"Inconsistent child {i}", otherList.Id, UserId, parentListItemId: parentItem.Id);
        }
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var childId = await service.CreateAsync(UserId, list.Id, parentItem.Id, "Valid child", null);

        var child = await context.ListItems.SingleAsync(li => li.Id == childId);
        Assert.Equal(list.Id, child.ParentId);
        Assert.Equal(UserId, child.OwnerId);
        Assert.Equal(parentItem.Id, child.ParentListItemId);
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
        var betaTask = AddItem(context, "Beta task", list.Id, UserId, isCompleted: true, dueDate: dueDate, sortIndex: 2);
        AddItem(context, "Gamma task", list.Id, UserId, sortIndex: 3);
        AddItem(context, "Other parent task", otherList.Id, UserId, sortIndex: 4);
        AddItem(context, "Other user task", list.Id, OtherUserId, sortIndex: 5);
        await context.SaveChangesAsync();
        var soonestChildDueDate = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc);
        AddItem(context, "Beta child 1", list.Id, UserId, isCompleted: true, dueDate: new DateTime(2026, 5, 14, 0, 0, 0, DateTimeKind.Utc), parentListItemId: betaTask.Id);
        AddItem(context, "Beta child 2", list.Id, UserId, dueDate: soonestChildDueDate, parentListItemId: betaTask.Id);
        AddItem(context, "Cross-list child", otherList.Id, UserId, isCompleted: true, dueDate: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), parentListItemId: betaTask.Id);
        AddItem(context, "Other-user child", list.Id, OtherUserId, isCompleted: true, dueDate: new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc), parentListItemId: betaTask.Id);
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
        Assert.True(item.IsCompleted);
        Assert.Equal(dueDate, item.DueDate);
        Assert.Equal(2, item.TotalChildren);
        Assert.Equal(1, item.TotalChildrenCompleted);
        Assert.Equal(soonestChildDueDate, item.SoonestChildDueDate);
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

        var result = await service.GetAsync(UserId, list.Id, item.Id);

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

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(UserId, list.Id, otherUserItem.Id));
    }

    [Fact]
    public async Task GetAsync_WhenItemHasNoChildren_ReturnsEmptyChildFields()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var item = AddItem(context, "Parent task", list.Id, UserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.GetAsync(UserId, list.Id, item.Id);

        Assert.False(result.HasChildren);
        Assert.Equal(0, result.TotalChildren);
        Assert.Equal(0, result.TotalChildrenCompleted);
        Assert.Null(result.SoonestChildDueDate);
    }

    [Fact]
    public async Task GetAsync_WhenItemHasChildren_ReturnsChildAggregates()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var otherList = AddList(context, "Backlog", UserId, category.Id);
        var parentItem = AddItem(context, "Parent task", list.Id, UserId);
        await context.SaveChangesAsync();
        
        var dueDate1 = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var dueDate2 = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc);
        AddItem(context, "Child 1", list.Id, UserId, isCompleted: true, dueDate: dueDate1, parentListItemId: parentItem.Id);
        AddItem(context, "Child 2", list.Id, UserId, isCompleted: false, dueDate: dueDate2, parentListItemId: parentItem.Id);
        AddItem(context, "Child 3", list.Id, UserId, isCompleted: true, parentListItemId: parentItem.Id);
        AddItem(context, "Cross-list child", otherList.Id, UserId, isCompleted: true, dueDate: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), parentListItemId: parentItem.Id);
        AddItem(context, "Other-user child", list.Id, OtherUserId, isCompleted: true, dueDate: new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc), parentListItemId: parentItem.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.GetAsync(UserId, list.Id, parentItem.Id);

        Assert.True(result.HasChildren);
        Assert.Equal(3, result.TotalChildren);
        Assert.Equal(2, result.TotalChildrenCompleted);
        Assert.Equal(dueDate2, result.SoonestChildDueDate);
    }

    [Fact]
    public async Task GetAsync_WhenChildrenHaveNoDueDates_ReturnsSoonestChildDueDateAsNull()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var parentItem = AddItem(context, "Parent task", list.Id, UserId);
        await context.SaveChangesAsync();
        
        AddItem(context, "Child 1", list.Id, UserId, parentListItemId: parentItem.Id);
        AddItem(context, "Child 2", list.Id, UserId, parentListItemId: parentItem.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.GetAsync(UserId, list.Id, parentItem.Id);

        Assert.True(result.HasChildren);
        Assert.Equal(2, result.TotalChildren);
        Assert.Null(result.SoonestChildDueDate);
    }

    [Fact]
    public async Task GetChildrenAsync_ReturnsAllChildrenOfParentItem()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var otherList = AddList(context, "Backlog", UserId, category.Id);
        var parentItem = AddItem(context, "Parent task", list.Id, UserId);
        await context.SaveChangesAsync();
        
        var dueDate = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var child1 = AddItem(context, "Child 1", list.Id, UserId, isCompleted: true, dueDate: dueDate, sortIndex: 1, parentListItemId: parentItem.Id);
        var child2 = AddItem(context, "Child 2", list.Id, UserId, isCompleted: false, sortIndex: 2, parentListItemId: parentItem.Id);
        AddItem(context, "Other item", list.Id, UserId);
        AddItem(context, "Cross-list child", otherList.Id, UserId, parentListItemId: parentItem.Id);
        AddItem(context, "Other-user child", list.Id, OtherUserId, parentListItemId: parentItem.Id);
        AddItem(context, "Grandchild", list.Id, UserId, isCompleted: true, dueDate: new DateTime(2026, 5, 14, 0, 0, 0, DateTimeKind.Utc), parentListItemId: child1.Id);
        AddItem(context, "Cross-list grandchild", otherList.Id, UserId, isCompleted: true, dueDate: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), parentListItemId: child1.Id);
        AddItem(context, "Other-user grandchild", list.Id, OtherUserId, isCompleted: true, dueDate: new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc), parentListItemId: child1.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.GetChildrenAsync(UserId, list.Id, parentItem.Id);

        Assert.Equal(2, result.Count);
        var firstChild = result.First(c => c.Id == child1.Id);
        Assert.Equal("Child 1", firstChild.Name);
        Assert.True(firstChild.IsCompleted);
        Assert.Equal(dueDate, firstChild.DueDate);
        Assert.Equal(1, firstChild.SortIndex);
        Assert.Equal(list.Id, firstChild.ParentId);
        Assert.Equal("Sprint", firstChild.ParentName);
        Assert.Equal("Work", firstChild.CategoryName);
        Assert.True(firstChild.HasChildren);
        Assert.Equal(1, firstChild.TotalChildren);
        Assert.Equal(1, firstChild.TotalChildrenCompleted);
        Assert.Equal(new DateTime(2026, 5, 14, 0, 0, 0, DateTimeKind.Utc), firstChild.SoonestChildDueDate);
        
        var secondChild = result.First(c => c.Id == child2.Id);
        Assert.Equal("Child 2", secondChild.Name);
        Assert.False(secondChild.IsCompleted);
        Assert.Equal(2, secondChild.SortIndex);
    }

    [Fact]
    public async Task GetChildrenAsync_WhenParentItemHasNoChildren_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var parentItem = AddItem(context, "Parent task", list.Id, UserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetChildrenAsync(UserId, list.Id, parentItem.Id));
    }

    [Fact]
    public async Task GetChildrenAsync_WhenParentItemNotFound_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetChildrenAsync(UserId, list.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetChildrenAsync_WhenUserDoesNotOwnParent_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        var otherUserItem = AddItem(context, "Other parent", list.Id, OtherUserId);
        await context.SaveChangesAsync();
        
        AddItem(context, "Child", list.Id, OtherUserId, parentListItemId: otherUserItem.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetChildrenAsync(UserId, list.Id, otherUserItem.Id));
    }

    [Fact]
    public async Task GetChildrenAsync_WhenParentIsInDifferentList_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list1 = AddList(context, "List 1", UserId, category.Id);
        var list2 = AddList(context, "List 2", UserId, category.Id);
        var parentItem = AddItem(context, "Parent", list1.Id, UserId);
        await context.SaveChangesAsync();
        
        AddItem(context, "Child", list1.Id, UserId, parentListItemId: parentItem.Id);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetChildrenAsync(UserId, list2.Id, parentItem.Id));
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

        await service.RenameAsync(UserId, list.Id, item.Id, "New");

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

        await service.SetDueDateAsync(UserId, list.Id, item.Id, dueDate);

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

        await service.ToggleCompletionAsync(UserId, list.Id, item.Id);
        context.ChangeTracker.Clear();
        Assert.True((await context.ListItems.FindAsync(item.Id))!.IsCompleted);

        await service.ToggleCompletionAsync(UserId, list.Id, item.Id);
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

        await service.DeleteAsync(UserId, list.Id, ownedItem.Id);

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

        await Assert.ThrowsAsync<NotFoundException>(() => service.RenameAsync(UserId, list.Id, otherUserItem.Id, "New"));
        await Assert.ThrowsAsync<NotFoundException>(() => service.SetDueDateAsync(UserId, list.Id, otherUserItem.Id, null));
        await Assert.ThrowsAsync<NotFoundException>(() => service.ToggleCompletionAsync(UserId, list.Id, otherUserItem.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(UserId, list.Id, otherUserItem.Id));
    }

    [Fact]
    public async Task ItemOperations_WhenItemIsInDifferentOwnedList_ThrowNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var itemList = AddList(context, "Item list", UserId, category.Id);
        var routeList = AddList(context, "Route list", UserId, category.Id);
        var item = AddItem(context, "Task", itemList.Id, UserId, dueDate: null);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(UserId, routeList.Id, item.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.RenameAsync(UserId, routeList.Id, item.Id, "New"));
        await Assert.ThrowsAsync<NotFoundException>(() => service.SetDueDateAsync(UserId, routeList.Id, item.Id, DateTime.UtcNow));
        await Assert.ThrowsAsync<NotFoundException>(() => service.ToggleCompletionAsync(UserId, routeList.Id, item.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(UserId, routeList.Id, item.Id));

        context.ChangeTracker.Clear();
        var unchangedItem = await context.ListItems.FindAsync(item.Id);
        Assert.NotNull(unchangedItem);
        Assert.Equal("Task", unchangedItem.Name);
        Assert.False(unchangedItem.IsCompleted);
        Assert.Null(unchangedItem.DueDate);
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
