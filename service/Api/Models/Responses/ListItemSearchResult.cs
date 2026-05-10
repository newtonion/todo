namespace Api.Models.Responses;

public class ListItemSearchResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public DateTime? DueDate { get; set; }
}
