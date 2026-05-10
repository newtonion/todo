using System;

namespace Api.Models.Responses;

public class ListGetResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool Archived { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public List<ListItemSearchResult> Items { get; set; } = new();
}
