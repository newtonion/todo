using Api.Infrastructure;
using Api.Infrastructure.Entities;

namespace Api.Tests.TestSupport;

public static class TestEntities
{
    public static UserEntity AddUser(TodoDatabaseContext context, string authId, string name, Guid? id = null)
    {
        var user = new UserEntity
        {
            Id = id ?? Guid.CreateVersion7(),
            AuthId = authId,
            Name = name
        };

        context.Users.Add(user);
        return user;
    }

    public static CategoryEntity AddCategory(TodoDatabaseContext context, string name, Guid? ownerId, DateTime? createdOn = null)
    {
        var category = new CategoryEntity
        {
            Name = name,
            OwnerId = ownerId,
            CreatedOn = createdOn ?? DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow
        };

        context.Categories.Add(category);
        return category;
    }

    public static ListEntity AddList(
        TodoDatabaseContext context,
        string name,
        Guid ownerId,
        Guid? categoryId,
        bool archived = false,
        bool isCompleted = false,
        DateTime? createdOn = null)
    {
        var list = new ListEntity
        {
            Name = name,
            OwnerId = ownerId,
            CategoryId = categoryId,
            Archived = archived,
            IsCompleted = isCompleted,
            CreatedOn = createdOn ?? DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow
        };

        context.Lists.Add(list);
        return list;
    }

    public static ListItemEntity AddItem(
        TodoDatabaseContext context,
        string name,
        Guid parentId,
        Guid ownerId,
        bool isCompleted = false,
        DateTime? dueDate = null,
        int sortIndex = 0,
        DateTime? createdOn = null)
    {
        var item = new ListItemEntity
        {
            Name = name,
            ParentId = parentId,
            OwnerId = ownerId,
            IsCompleted = isCompleted,
            DueDate = dueDate,
            SortIndex = sortIndex,
            CreatedOn = createdOn ?? DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow
        };

        context.ListItems.Add(item);
        return item;
    }
}
