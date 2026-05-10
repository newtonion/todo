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
}
