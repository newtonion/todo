using System;
using Api.Domain.Exceptions;
using Api.Infrastructure.Entities;

namespace Api.Validators;

public class CategoryEntityValidator: IEntityValidator<CategoryEntity>
{
    public Task ValidateAsync(CategoryEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            throw new ValidationException("Category name cannot be empty or whitespace.");
        }

        if (entity.Name.Length > CategoryEntity.MaxNameLength)
        {
            throw new ValidationException($"Category name cannot exceed {CategoryEntity.MaxNameLength} characters.");
        }

        return Task.CompletedTask;
    }
}
