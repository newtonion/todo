using Api.Infrastructure.Extensions;
using Api.Models.Requests;
using Api.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using static Api.Tests.TestSupport.TestEntities;
using Xunit;

namespace Api.Tests.Extensions;

public class ListEntityExtensionsTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task WhereCurrentUserHasAccess_ReturnsOnlyOwnedLists()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        AddList(context, "Owned", UserId, category.Id);
        AddList(context, "Other", OtherUserId, category.Id);
        await context.SaveChangesAsync();

        var name = await context.Lists
            .WhereCurrentUserHasAccess(UserId)
            .Select(l => l.Name)
            .SingleAsync();

        Assert.Equal("Owned", name);
    }

    [Fact]
    public async Task WhereArchived_WhenIncludeArchivedIsNotTrue_ReturnsOnlyOpenLists()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        AddList(context, "Open", UserId, category.Id);
        AddList(context, "Closed", UserId, category.Id, archived: true);
        await context.SaveChangesAsync();

        Assert.Equal(["Open"], await context.Lists.WhereArchived(null).Select(l => l.Name).ToListAsync());
        Assert.Equal(["Open"], await context.Lists.WhereArchived(false).Select(l => l.Name).ToListAsync());
    }

    [Fact]
    public async Task WhereArchived_WhenIncludeArchivedIsTrue_DoesNotFilter()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        AddList(context, "Open", UserId, category.Id);
        AddList(context, "Closed", UserId, category.Id, archived: true);
        await context.SaveChangesAsync();

        var names = await context.Lists
            .WhereArchived(true)
            .OrderBy(l => l.Name)
            .Select(l => l.Name)
            .ToListAsync();

        Assert.Equal(["Closed", "Open"], names);
    }

    [Fact]
    public async Task WhereSearchCriteria_AppliesUserNameAndArchivedFilters()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        AddList(context, "Alpha project", UserId, category.Id);
        AddList(context, "Beta project", UserId, category.Id, archived: true);
        AddList(context, "Other project", OtherUserId, category.Id);
        AddList(context, "No match", UserId, category.Id);
        await context.SaveChangesAsync();

        var names = await context.Lists
            .WhereSearchCriteria(new ListSearchCriteria { Text = "project" }, UserId)
            .Select(l => l.Name)
            .ToListAsync();

        var name = Assert.Single(names);
        Assert.Equal("Alpha project", name);
    }

    [Fact]
    public async Task WhereUpcomingOrOverdue_WhenFalse_ReturnsAllLists()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var listWithOverdueItem = AddList(context, "Overdue", UserId, category.Id);
        var listWithNoItems = AddList(context, "NoItems", UserId, category.Id);
        await context.SaveChangesAsync();

        AddItem(context, "Overdue task", listWithOverdueItem.Id, UserId, dueDate: DateTime.UtcNow.AddDays(-1));
        await context.SaveChangesAsync();

        var names = await context.Lists
            .WhereUpcomingOrOverdue(false)
            .OrderBy(l => l.Name)
            .Select(l => l.Name)
            .ToListAsync();

        Assert.Equal(["NoItems", "Overdue"], names);
    }

    [Fact]
    public async Task WhereUpcomingOrOverdue_WhenNull_ReturnsAllLists()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var listWithOverdueItem = AddList(context, "Overdue", UserId, category.Id);
        var listWithNoItems = AddList(context, "NoItems", UserId, category.Id);
        await context.SaveChangesAsync();

        AddItem(context, "Overdue task", listWithOverdueItem.Id, UserId, dueDate: DateTime.UtcNow.AddDays(-1));
        await context.SaveChangesAsync();

        var names = await context.Lists
            .Include(l => l.Children)
            .WhereUpcomingOrOverdue(null)
            .OrderBy(l => l.Name)
            .Select(l => l.Name)
            .ToListAsync();

        Assert.Equal(["NoItems", "Overdue"], names);
    }

    [Fact]
    public async Task WhereUpcomingOrOverdue_WhenTrue_ReturnsListsWithOverdueItems()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var listWithOverdueItem = AddList(context, "Overdue", UserId, category.Id);
        var listWithFutureItem = AddList(context, "Future", UserId, category.Id);
        await context.SaveChangesAsync();

        AddItem(context, "Overdue task", listWithOverdueItem.Id, UserId, dueDate: DateTime.UtcNow.AddDays(-1));
        AddItem(context, "Future task", listWithFutureItem.Id, UserId, dueDate: DateTime.UtcNow.AddDays(5));
        await context.SaveChangesAsync();

        var names = await context.Lists
            .Include(l => l.Children)
            .WhereUpcomingOrOverdue(true)
            .Select(l => l.Name)
            .ToListAsync();

        var name = Assert.Single(names);
        Assert.Equal("Overdue", name);
    }

    [Fact]
    public async Task WhereUpcomingOrOverdue_WhenTrue_ReturnsListsWithUpcomingItemsWithinTwoDays()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var listDueToday = AddList(context, "DueToday", UserId, category.Id);
        var listDueInOneDay = AddList(context, "DueInOneDay", UserId, category.Id);
        var listDueInTwoDays = AddList(context, "DueInTwoDays", UserId, category.Id);
        var listDueInThreeDays = AddList(context, "DueInThreeDays", UserId, category.Id);
        await context.SaveChangesAsync();

        AddItem(context, "Due today", listDueToday.Id, UserId, dueDate: DateTime.UtcNow);
        AddItem(context, "Due in 1 day", listDueInOneDay.Id, UserId, dueDate: DateTime.UtcNow.AddDays(1));
        AddItem(context, "Due in 2 days", listDueInTwoDays.Id, UserId, dueDate: DateTime.UtcNow.AddDays(2));
        AddItem(context, "Due in 3 days", listDueInThreeDays.Id, UserId, dueDate: DateTime.UtcNow.AddDays(3));
        await context.SaveChangesAsync();

        var names = await context.Lists
            .Include(l => l.Children)
            .WhereUpcomingOrOverdue(true)
            .OrderBy(l => l.Name)
            .Select(l => l.Name)
            .ToListAsync();

        Assert.Equal(["DueInOneDay", "DueInTwoDays", "DueToday"], names);
    }

    [Fact]
    public async Task WhereUpcomingOrOverdue_WhenTrue_ExcludesListsWithNoDueDates()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var listWithDueDate = AddList(context, "WithDueDate", UserId, category.Id);
        var listWithoutDueDate = AddList(context, "WithoutDueDate", UserId, category.Id);
        await context.SaveChangesAsync();

        AddItem(context, "Has due date", listWithDueDate.Id, UserId, dueDate: DateTime.UtcNow.AddDays(1));
        AddItem(context, "No due date", listWithoutDueDate.Id, UserId, dueDate: null);
        await context.SaveChangesAsync();

        var names = await context.Lists
            .Include(l => l.Children)
            .WhereUpcomingOrOverdue(true)
            .Select(l => l.Name)
            .ToListAsync();

        var name = Assert.Single(names);
        Assert.Equal("WithDueDate", name);
    }

    [Fact]
    public async Task WhereUpcomingOrOverdue_WhenTrue_ExcludesListsWithNoItems()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var listWithItems = AddList(context, "WithItems", UserId, category.Id);
        var listWithoutItems = AddList(context, "WithoutItems", UserId, category.Id);
        await context.SaveChangesAsync();

        AddItem(context, "Upcoming task", listWithItems.Id, UserId, dueDate: DateTime.UtcNow.AddDays(1));
        await context.SaveChangesAsync();

        var names = await context.Lists
            .Include(l => l.Children)
            .WhereUpcomingOrOverdue(true)
            .Select(l => l.Name)
            .ToListAsync();

        var name = Assert.Single(names);
        Assert.Equal("WithItems", name);
    }

    [Fact]
    public async Task WhereUpcomingOrOverdue_WhenTrue_IncludesListIfAnyItemIsUpcoming()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var category = AddCategory(context, "Work", UserId);
        var list = AddList(context, "MixedItems", UserId, category.Id);
        await context.SaveChangesAsync();

        AddItem(context, "No due date", list.Id, UserId, dueDate: null);
        AddItem(context, "Future task", list.Id, UserId, dueDate: DateTime.UtcNow.AddDays(5));
        AddItem(context, "Upcoming task", list.Id, UserId, dueDate: DateTime.UtcNow.AddDays(1));
        await context.SaveChangesAsync();

        var names = await context.Lists
            .Include(l => l.Children)
            .WhereUpcomingOrOverdue(true)
            .Select(l => l.Name)
            .ToListAsync();

        var name = Assert.Single(names);
        Assert.Equal("MixedItems", name);
    }
}
