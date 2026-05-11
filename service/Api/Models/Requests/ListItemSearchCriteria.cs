using System;
using System.ComponentModel.DataAnnotations;

namespace Api.Models.Requests;

public class ListItemSearchCriteria
{
    public Guid? ListId { get; set; }
    
    [StringLength(200, ErrorMessage = "Search text must be between 1 and 200 characters")]
    public string? Text { get; set; }
    
    public FieldOrderRequest? OrderBy { get; set; }
    
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int? PageSize { get; set; }
    
    [Range(0, int.MaxValue, ErrorMessage = "Offset must be non-negative")]
    public int? Offset { get; set; }
}