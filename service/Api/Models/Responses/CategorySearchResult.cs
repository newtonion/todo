using System;

namespace Api.Models.Responses;

public class CategorySearchResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
