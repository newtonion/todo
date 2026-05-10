using System.ComponentModel.DataAnnotations;

namespace Api.Models.Requests;

public class ListSearchCriteria()
{
    public string? Text { get; set; }
    
    public FieldOrderRequest? OrderBy { get; set; }
    
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int? PageSize { get; set; }
    
    [Range(0, int.MaxValue, ErrorMessage = "Offset must be non-negative")]
    public int? Offset { get; set; }
    
    public bool? IncludeArchived { get; set; }
}

