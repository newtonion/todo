namespace Api.Models.Responses;

public class ListItemSearchResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? DueDate { get; set; }
    public int TotalChildren { get; set; }
    public int TotalChildrenCompleted { get; set; }
    public DateTime? SoonestChildDueDate { get; set; }
}
