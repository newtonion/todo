using System;
using Api.Domain.Exceptions;
using Api.Infrastructure.Entities;

namespace Api.Validators;

public class ListEntityValidator: IEntityValidator<ListEntity>
{
    public Task ValidateAsync(ListEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            throw new ValidationException("List name cannot be empty or whitespace.");
        }

        if (entity.Name.Length > ListEntity.MaxNameLength)
        {
            throw new ValidationException($"List name cannot exceed {ListEntity.MaxNameLength} characters.");
        }

        return Task.CompletedTask;
    }
}
