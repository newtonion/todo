using System.ComponentModel.DataAnnotations;

namespace Api.Models.Requests;

public class CategorySearchCriteria
{
    [StringLength(100, ErrorMessage = "Search text must be between 1 and 100 characters")]
    public string? Text { get; set; }
    
    public FieldOrderRequest? OrderBy { get; set; }
    
}
