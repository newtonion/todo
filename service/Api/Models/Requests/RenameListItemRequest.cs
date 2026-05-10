using System.ComponentModel.DataAnnotations;

namespace Api.Models.Requests;

public class RenameListItemRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Item name must be between 1 and 500 characters")]
    public required string Name { get; set; }
}
