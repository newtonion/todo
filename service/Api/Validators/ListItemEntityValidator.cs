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
        
        if (entity.ParentListItem != null && entity.ParentListItem.Children.Count() > 10)
        {
            throw new ValidationException("A list item cannot have more than 10 child items.");
        }

        return Task.CompletedTask;
    }
}
