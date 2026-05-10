namespace Api.Models.Requests;

public class CategorySearchCriteria
{
    public string? Text { get; set; }
    
    public FieldOrderRequest? OrderBy { get; set; }
    
}
