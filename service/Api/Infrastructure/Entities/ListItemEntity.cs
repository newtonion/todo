using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Entities;

[Index(nameof(Owner), nameof(Name), Name = "IX_ListItem_Owner_Name")]
public class ListItemEntity: MutableEntity
{
    public const int MaxNameLength = 500;

    public bool IsCompleted { get; set; }
    public DateTime? DueDate { get; set; } = null;
    [ForeignKey("Owner")]
    public Guid OwnerId { get; set; }

    [ForeignKey("Parent")]
    public Guid ParentId { get; set; }
    public int SortIndex {get; set;}
    
    [MaxLength(MaxNameLength)]
    public required string Name { get; set; }

    // Navigation properties
    public virtual ListEntity Parent { get; set; } = null!;
    public virtual UserEntity Owner {get; set;} = null!;

    // Mappings for sorting
    public static readonly Dictionary<string, Expression<Func<ListItemEntity, object>>> SortMappings =
    new()
    {
        ["dueDate"] = x => x.DueDate ?? DateTime.MaxValue,
        ["completed"] = x => x.IsCompleted,
        ["customSort"] = x => x.SortIndex,
        ["name"] = x => x.Name
    };
}
