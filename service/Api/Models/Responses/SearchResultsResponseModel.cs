namespace Api.Models.Responses;

public class SearchResultsResponseModel<T>
    where T : class
{
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int Offset { get; set; }
    public List<T> Items { get; set; } = new List<T>();
}
