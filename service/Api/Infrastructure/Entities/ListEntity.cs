using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Entities;

[Index(nameof(Owner), nameof(Name), Name = "IX_List_Owner_Name")]
public class ListEntity : MutableEntity
{
    public const int MaxNameLength = 200;

    public bool Archived { get; set; } = false;
    public bool IsCompleted { get; set; }
    [ForeignKey("Category")]
    public Guid? CategoryId { get; set; }
    [ForeignKey("Owner")]
    public Guid OwnerId { get; set; }
    
    [MaxLength(MaxNameLength)]
    public required string Name { get; set; }
    
    // Navigation properties
    public virtual List<ListItemEntity> Children { get; set; } = new() ;
    public virtual CategoryEntity? Category {get; set;} = null!;
    public virtual UserEntity Owner {get; set;} = null!;

    // Mappings for sorting
    public static readonly Dictionary<string, Expression<Func<ListEntity, object>>> SortMappings =
    new()
    {
        ["id"] = x => x.Id,
        ["category"] = x => x.Category != null ? x.Category.Name : string.Empty,
        ["createdOn"] = x => x.CreatedOn,
        ["completed"] = x => x.IsCompleted,
        ["archived"] = x => x.Archived,
        ["children"] = x => x.Children.Count
    };
}


