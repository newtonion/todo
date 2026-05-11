using System.ComponentModel.DataAnnotations;

namespace Api.Models.Requests;

public class ListSearchCriteria()
{
    [StringLength(200, ErrorMessage = "Search Text cannot be longer than 200 characters")]
    public string? Text { get; set; }
    [StringLength(200, ErrorMessage = "Category Text cannot be longer than 200 characters")]
    public string? CategoryText { get; set; }
    
    public FieldOrderRequest? OrderBy { get; set; }
    
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int? PageSize { get; set; }
    
    [Range(0, int.MaxValue, ErrorMessage = "Offset must be non-negative")]
    public int? Offset { get; set; }
    
    public bool? IncludeArchived { get; set; }

    public bool? IncludeCompleted { get; set; }
    
    public bool? OnlyUpcomingOrOverdue { get; set; }
}
