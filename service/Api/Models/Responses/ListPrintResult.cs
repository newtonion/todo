namespace Api.Models.Responses;

public class ListPrintResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ListPrintItemResult> Items { get; set; } = new List<ListPrintItemResult>();
}
