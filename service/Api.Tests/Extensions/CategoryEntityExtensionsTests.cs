using Api.Infrastructure.Extensions;
using Api.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using static Api.Tests.TestSupport.TestEntities;
using Xunit;

namespace Api.Tests.Extensions;

public class CategoryEntityExtensionsTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task WhereCurrentUserHasAccess_ReturnsGlobalAndOwnedCategories()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        AddCategory(context, "Global", null);
        AddCategory(context, "Owned", UserId);
        AddCategory(context, "Other", OtherUserId);
        await context.SaveChangesAsync();

        var names = await context.Categories
            .WhereCurrentUserHasAccess(UserId)
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync();

        Assert.Equal(["Global", "Owned"], names);
    }

    [Fact]
    public async Task WhereOwnedByUser_ReturnsOnlyOwnedCategories()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        AddCategory(context, "Global", null);
        AddCategory(context, "Owned", UserId);
        AddCategory(context, "Other", OtherUserId);
        await context.SaveChangesAsync();

        var names = await context.Categories
            .WhereOwnedByUser(UserId)
            .Select(c => c.Name)
            .ToListAsync();

        var name = Assert.Single(names);
        Assert.Equal("Owned", name);
    }

    [Fact]
    public async Task WhereName_WhenCriteriaIsNullOrEmpty_DoesNotFilter()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        AddCategory(context, "Work", UserId);
        AddCategory(context, "Home", UserId);
        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Categories.WhereName(null).CountAsync());
        Assert.Equal(2, await context.Categories.WhereName(string.Empty).CountAsync());
    }

    [Fact]
    public async Task WhereName_WhenCriteriaIsProvided_FiltersByName()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        AddCategory(context, "Work", UserId);
        AddCategory(context, "Home", UserId);
        AddCategory(context, "Personal", UserId);
        await context.SaveChangesAsync();

        var names = await context.Categories
            .WhereName("o")
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync();

        Assert.Equal(["Home", "Personal", "Work"], names);
    }
}
