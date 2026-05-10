using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Api.Infrastructure.Entities;

public class CategoryEntity : MutableEntity
{
    public const int MaxNameLength = 100;
    public UserEntity? Owner { get; set; }
    public Guid? OwnerId { get; set; }
    
    [MaxLength(MaxNameLength)]
    public required string Name { get; set; }

    // Mappings for sorting
    public static readonly Dictionary<string, Expression<Func<CategoryEntity, object>>> SortMappings =
    new()
    {
        ["name"] = x => x.Name,
        ["createdOn"] = x => x.CreatedOn
    };
}
