using System;

namespace Api.Models.Responses;

public class ListSearchResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool Archived { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
}
