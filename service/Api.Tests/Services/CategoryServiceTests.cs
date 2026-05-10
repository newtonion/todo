using Api.Infrastructure.Entities;
using Api.Tests.TestSupport;
using static Api.Tests.TestSupport.TestEntities;
using Api.Models.Requests;
using Api.Services;
using Api.Validators;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Api.Tests.Services;

public class CategoryServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task SearchAsync_ReturnsAccessibleCategories()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        AddCategory(context, "Work", UserId);
        AddCategory(context, "Shopping", null);
        AddCategory(context, "Projects", UserId);
        AddCategory(context, "Personal", OtherUserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.SearchAsync(UserId, new CategorySearchCriteria
        {
            Text = "o"
        });

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(["Projects", "Shopping", "Work"], result.Items.Select(c => c.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task SearchAsync_WhenOrderByProvided_AppliesRequestedSort()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        AddCategory(context, "Alpha", UserId, createdOn: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        AddCategory(context, "Bravo", UserId, createdOn: new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc));
        AddCategory(context, "Charlie", null, createdOn: new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var result = await service.SearchAsync(UserId, new CategorySearchCriteria
        {
            OrderBy = new FieldOrderRequest
            {
                Field = "createdOn",
                Ascending = false
            }
        });

        Assert.Equal(["Bravo", "Charlie", "Alpha"], result.Items.Select(c => c.Name));
    }

    private static CategoryService CreateService(TestDatabase database)
    {
        return new CategoryService(
            new TestDbContextFactory(database.Options),
            new CategoryEntityValidator(),
            NullLogger<CategoryService>.Instance);
    }

}
