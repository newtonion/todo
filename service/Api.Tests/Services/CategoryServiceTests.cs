using Api.Domain.Exceptions;
using Api.Infrastructure.Entities;
using Api.Tests.TestSupport;
using static Api.Tests.TestSupport.TestEntities;
using Api.Models.Requests;
using Api.Services;
using Api.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Api.Tests.Services;

public class CategoryServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task CreateAsync_PersistsCategoryOwnedByUser()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var validator = new Mock<IEntityValidator<CategoryEntity>>();
        var service = CreateService(database, validator.Object);

        var categoryId = await service.CreateAsync(UserId, "Errands");

        var category = await context.Categories.SingleAsync(c => c.Id == categoryId);
        Assert.Equal(UserId, category.OwnerId);
        Assert.Equal("Errands", category.Name);
        Assert.NotEqual(default, category.CreatedOn);
        Assert.NotEqual(default, category.UpdatedOn);
        validator.Verify(v => v.ValidateAsync(It.Is<CategoryEntity>(c =>
            c.Id == categoryId &&
            c.OwnerId == UserId &&
            c.Name == "Errands")), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenValidationFails_DoesNotPersistCategory()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var validator = new Mock<IEntityValidator<CategoryEntity>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<CategoryEntity>()))
            .ThrowsAsync(new ValidationException("Invalid category"));
        var service = CreateService(database, validator.Object);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(UserId, " "));

        Assert.Empty(await context.Categories.ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyCategoryOwnedByUser()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var ownedCategory = AddCategory(context, "Home", UserId);
        var globalCategory = AddCategory(context, "Global", null);
        var otherUserCategory = AddCategory(context, "Other", OtherUserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await service.UpdateAsync(UserId, ownedCategory.Id, "Personal");

        context.ChangeTracker.Clear();
        Assert.Equal("Personal", (await context.Categories.FindAsync(ownedCategory.Id))!.Name);
        Assert.Equal("Global", (await context.Categories.FindAsync(globalCategory.Id))!.Name);
        Assert.Equal("Other", (await context.Categories.FindAsync(otherUserCategory.Id))!.Name);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryIsNotOwnedByUser_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var globalCategory = AddCategory(context, "Global", null);
        var otherUserCategory = AddCategory(context, "Other", OtherUserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(UserId, globalCategory.Id, "New Global"));
        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(UserId, otherUserCategory.Id, "New Other"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyCategoryOwnedByUser()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var ownedCategory = AddCategory(context, "Home", UserId);
        var globalCategory = AddCategory(context, "Global", null);
        var otherUserCategory = AddCategory(context, "Other", OtherUserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await service.DeleteAsync(UserId, ownedCategory.Id);

        context.ChangeTracker.Clear();
        Assert.Null(await context.Categories.FindAsync(ownedCategory.Id));
        Assert.NotNull(await context.Categories.FindAsync(globalCategory.Id));
        Assert.NotNull(await context.Categories.FindAsync(otherUserCategory.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryIsNotOwnedByUser_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var globalCategory = AddCategory(context, "Global", null);
        var otherUserCategory = AddCategory(context, "Other", OtherUserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(UserId, globalCategory.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(UserId, otherUserCategory.Id));
    }

    [Fact]
    public async Task SearchAsync_ReturnsAccessibleCategoriesFilteredSortedAndPaged()
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
            Text = "o",
            PageSize = 2,
            Offset = 1
        });

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(1, result.Offset);
        Assert.Equal(["Shopping", "Work"], result.Items.Select(c => c.Name));
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

    [Fact]
    public async Task GetAsync_ReturnsUserOwnedAndGlobalCategories()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var ownedCategory = AddCategory(context, "Home", UserId);
        var globalCategory = AddCategory(context, "Global", null);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        var ownedResult = await service.GetAsync(UserId, ownedCategory.Id);
        var globalResult = await service.GetAsync(UserId, globalCategory.Id);

        Assert.Equal("Home", ownedResult.Name);
        Assert.Equal("Global", globalResult.Name);
    }

    [Fact]
    public async Task GetAsync_WhenCategoryIsNotAccessible_ThrowsNotFoundException()
    {
        var database = new TestDatabase();
        await using var context = database.CreateContext();
        var otherUserCategory = AddCategory(context, "Other", OtherUserId);
        await context.SaveChangesAsync();
        var service = CreateService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(UserId, otherUserCategory.Id));
    }

    private static CategoryService CreateService(
        TestDatabase database,
        IEntityValidator<CategoryEntity>? validator = null)
    {
        return new CategoryService(
            new TestDbContextFactory(database.Options),
            validator ?? new CategoryEntityValidator(),
            NullLogger<CategoryService>.Instance);
    }

}
