namespace Api.Models.Responses;

public class ListPrintItemResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }

    public List<ListPrintItemResult> SubItems { get; set; } = new List<ListPrintItemResult>();
}
