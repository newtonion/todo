using Api.Domain.Exceptions;
using Api.Infrastructure.Entities;
using Api.Validators;
using Xunit;

namespace Api.Tests.Validators;

public class EntityValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task CategoryEntityValidator_WhenNameIsBlank_ThrowsValidationException(string name)
    {
        var validator = new CategoryEntityValidator();
        var category = new CategoryEntity { Name = name };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => validator.ValidateAsync(category));

        Assert.Equal("Category name cannot be empty or whitespace.", exception.Message);
    }

    [Fact]
    public async Task CategoryEntityValidator_WhenNameExceedsMaxLength_ThrowsValidationException()
    {
        var validator = new CategoryEntityValidator();
        var category = new CategoryEntity { Name = new string('a', CategoryEntity.MaxNameLength + 1) };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => validator.ValidateAsync(category));

        Assert.Equal($"Category name cannot exceed {CategoryEntity.MaxNameLength} characters.", exception.Message);
    }

    [Fact]
    public async Task CategoryEntityValidator_WhenNameIsValid_Completes()
    {
        var validator = new CategoryEntityValidator();
        var category = new CategoryEntity { Name = new string('a', CategoryEntity.MaxNameLength) };

        await validator.ValidateAsync(category);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task ListEntityValidator_WhenNameIsBlank_ThrowsValidationException(string name)
    {
        var validator = new ListEntityValidator();
        var list = new ListEntity { Name = name };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => validator.ValidateAsync(list));

        Assert.Equal("List name cannot be empty or whitespace.", exception.Message);
    }

    [Fact]
    public async Task ListEntityValidator_WhenNameExceedsMaxLength_ThrowsValidationException()
    {
        var validator = new ListEntityValidator();
        var list = new ListEntity { Name = new string('a', ListEntity.MaxNameLength + 1) };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => validator.ValidateAsync(list));

        Assert.Equal($"List name cannot exceed {ListEntity.MaxNameLength} characters.", exception.Message);
    }

    [Fact]
    public async Task ListEntityValidator_WhenNameIsValid_Completes()
    {
        var validator = new ListEntityValidator();
        var list = new ListEntity { Name = new string('a', ListEntity.MaxNameLength) };

        await validator.ValidateAsync(list);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task ListItemEntityValidator_WhenNameIsBlank_ThrowsValidationException(string name)
    {
        var validator = new ListItemEntityValidator();
        var item = new ListItemEntity { Name = name };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => validator.ValidateAsync(item));

        Assert.Equal("List item name cannot be empty or whitespace.", exception.Message);
    }

    [Fact]
    public async Task ListItemEntityValidator_WhenNameExceedsMaxLength_ThrowsValidationException()
    {
        var validator = new ListItemEntityValidator();
        var item = new ListItemEntity { Name = new string('a', ListItemEntity.MaxNameLength + 1) };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => validator.ValidateAsync(item));

        Assert.Equal($"List item name cannot exceed {ListItemEntity.MaxNameLength} characters.", exception.Message);
    }

    [Fact]
    public async Task ListItemEntityValidator_WhenNameIsValid_Completes()
    {
        var validator = new ListItemEntityValidator();
        var item = new ListItemEntity { Name = new string('a', ListItemEntity.MaxNameLength) };

        await validator.ValidateAsync(item);
    }
}
