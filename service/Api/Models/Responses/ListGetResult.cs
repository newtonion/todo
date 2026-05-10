namespace Api.Models.Responses;

public class ListGetResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public bool Archived { get; set; }
    public bool Completed { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
}
