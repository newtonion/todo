using Api.Infrastructure.Extensions;
using Api.Infrastructure.Entities;
using Api.Models.Requests;
using Api.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using static Api.Tests.TestSupport.TestEntities;
using Xunit;

namespace Api.Tests.Extensions;

public class ListItemEntityExtensionsTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task WhereCurrentUserHasAccess_ReturnsOnlyOwnedItems()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        AddItem(context, "Owned", list.Id, UserId);
        AddItem(context, "Other", list.Id, OtherUserId);
        await context.SaveChangesAsync();

        var name = await context.ListItems
            .WhereCurrentUserHasAccess(UserId)
            .Select(li => li.Name)
            .SingleAsync();

        Assert.Equal("Owned", name);
    }

    [Fact]
    public async Task WhereParent_WhenParentIdIsProvided_FiltersByParent()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var firstList = AddList(context, "Sprint", UserId, category.Id);
        var secondList = AddList(context, "Inbox", UserId, category.Id);
        AddItem(context, "First", firstList.Id, UserId);
        AddItem(context, "Second", secondList.Id, UserId);
        await context.SaveChangesAsync();

        var name = await context.ListItems
            .WhereParent(firstList.Id)
            .Select(li => li.Name)
            .SingleAsync();

        Assert.Equal("First", name);
    }

    [Fact]
    public async Task WhereSearchCriteria_AppliesUserParentAndNameFilters()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var firstList = AddList(context, "Sprint", UserId, category.Id);
        var secondList = AddList(context, "Inbox", UserId, category.Id);
        AddItem(context, "Alpha task", firstList.Id, UserId);
        AddItem(context, "Beta task", firstList.Id, UserId);
        AddItem(context, "Other parent task", secondList.Id, UserId);
        AddItem(context, "Other user task", firstList.Id, OtherUserId);
        await context.SaveChangesAsync();

        var names = await context.ListItems
            .WhereSearchCriteria(new ListItemSearchCriteria { ListId = firstList.Id, Text = "task" }, UserId)
            .OrderBy(li => li.Name)
            .Select(li => li.Name)
            .ToListAsync();

        Assert.Equal(["Alpha task", "Beta task"], names);
    }

    [Fact]
    public async Task SortEntity_StatusTreatsSameDayDueDateAsPending()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "Sprint", UserId, category.Id);
        AddItem(context, "Completed", list.Id, UserId, isCompleted: true, dueDate: DateTime.UtcNow.AddDays(-1));
        AddItem(context, "Overdue", list.Id, UserId, dueDate: DateTime.UtcNow.Date.AddDays(-1));
        AddItem(context, "Today", list.Id, UserId, dueDate: DateTime.UtcNow.Date);
        await context.SaveChangesAsync();

        var names = await context.ListItems
            .WhereParent(list.Id)
            .SortEntity(
            [
                new FieldOrderRequest { Field = "status", Ascending = true },
                new FieldOrderRequest { Field = "name", Ascending = true }
            ], ListItemEntity.SortMappings)
            .Select(li => li.Name)
            .ToListAsync();

        Assert.Equal(["Completed", "Overdue", "Today"], names);
    }
}
