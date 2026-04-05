namespace blazor_wasm.Models;

/// <summary>
/// Paginated response wrapper
/// </summary>
/// <typeparam name="T">The type of items in the page</typeparam>
public class PaginationDto<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}
