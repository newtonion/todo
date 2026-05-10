using Api.Infrastructure.Entities;
using Api.Infrastructure.Extensions;
using Api.Models.Requests;
using Api.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using static Api.Tests.TestSupport.TestEntities;
using Xunit;

namespace Api.Tests.Extensions;

public class GenericEntityExtensionsTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task SortEntity_WhenNoSortsAreProvided_ReturnsOriginalOrder()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        AddCategory(context, "Bravo", UserId);
        AddCategory(context, "Alpha", UserId);
        await context.SaveChangesAsync();

        var names = await context.Categories
            .SortEntity([], CategoryEntity.SortMappings)
            .Select(c => c.Name)
            .ToListAsync();

        Assert.Equal(["Bravo", "Alpha"], names);
    }

    [Fact]
    public async Task SortEntity_WhenSortFieldIsUnknown_IgnoresSort()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        AddCategory(context, "Bravo", UserId);
        AddCategory(context, "Alpha", UserId);
        await context.SaveChangesAsync();

        var names = await context.Categories
            .SortEntity([new FieldOrderRequest { Field = "unknown" }], CategoryEntity.SortMappings)
            .Select(c => c.Name)
            .ToListAsync();

        Assert.Equal(["Bravo", "Alpha"], names);
    }

    [Fact]
    public async Task SortEntity_WhenSortFieldIsKnown_AppliesAscendingSort()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        AddCategory(context, "Bravo", UserId);
        AddCategory(context, "Alpha", UserId);
        await context.SaveChangesAsync();

        var names = await context.Categories
            .SortEntity([new FieldOrderRequest { Field = "name", Ascending = true }], CategoryEntity.SortMappings)
            .Select(c => c.Name)
            .ToListAsync();

        Assert.Equal(["Alpha", "Bravo"], names);
    }

    [Fact]
    public async Task SortEntity_WhenSortFieldIsKnown_AppliesDescendingSort()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        AddCategory(context, "Alpha", UserId);
        AddCategory(context, "Bravo", UserId);
        await context.SaveChangesAsync();

        var names = await context.Categories
            .SortEntity([new FieldOrderRequest { Field = "name", Ascending = false }], CategoryEntity.SortMappings)
            .Select(c => c.Name)
            .ToListAsync();

        Assert.Equal(["Bravo", "Alpha"], names);
    }

    [Fact]
    public async Task SortEntity_WhenMultipleSortsAreProvided_AppliesThenBySorts()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        AddCategory(context, "Bravo", UserId, createdOn: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        AddCategory(context, "Charlie", UserId, createdOn: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        AddCategory(context, "Alpha", UserId, createdOn: new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        await context.SaveChangesAsync();

        var names = await context.Categories
            .SortEntity(
                [
                    new FieldOrderRequest { Field = "createdOn", Ascending = true },
                    new FieldOrderRequest { Field = "name", Ascending = false }
                ],
                CategoryEntity.SortMappings)
            .Select(c => c.Name)
            .ToListAsync();

        Assert.Equal(["Charlie", "Bravo", "Alpha"], names);
    }

    [Fact]
    public async Task SortEntity_WhenUnknownSortFollowsKnownSort_IgnoresUnknownSort()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        AddCategory(context, "Bravo", UserId);
        AddCategory(context, "Alpha", UserId);
        await context.SaveChangesAsync();

        var names = await context.Categories
            .SortEntity(
                [
                    new FieldOrderRequest { Field = "name", Ascending = true },
                    new FieldOrderRequest { Field = "unknown", Ascending = false }
                ],
                CategoryEntity.SortMappings)
            .Select(c => c.Name)
            .ToListAsync();

        Assert.Equal(["Alpha", "Bravo"], names);
    }

    [Fact]
    public async Task SortEntity_WhenUnknownSortPrecedesKnownSort_IgnoresUnknownSortAndAppliesKnownSort()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        AddCategory(context, "Bravo", UserId);
        AddCategory(context, "Alpha", UserId);
        await context.SaveChangesAsync();

        var names = await context.Categories
            .SortEntity(
                [
                    new FieldOrderRequest { Field = "unknown", Ascending = false },
                    new FieldOrderRequest { Field = "name", Ascending = true }
                ],
                CategoryEntity.SortMappings)
            .Select(c => c.Name)
            .ToListAsync();

        Assert.Equal(["Alpha", "Bravo"], names);
    }
}
