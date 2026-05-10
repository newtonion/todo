using System;
using Api.Domain.Exceptions;
using Api.Infrastructure.Entities;

namespace Api.Validators;

public class ListItemEntityValidator: IEntityValidator<ListItemEntity>
{
    public Task ValidateAsync(ListItemEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            throw new ValidationException("List item name cannot be empty or whitespace.");
        }

        if (entity.Name.Length > ListItemEntity.MaxNameLength)
        {
            throw new ValidationException($"List item name cannot exceed {ListItemEntity.MaxNameLength} characters.");
        }

        return Task.CompletedTask;
    }
}
