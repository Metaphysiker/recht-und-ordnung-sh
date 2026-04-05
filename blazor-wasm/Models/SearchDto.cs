namespace blazor_wasm.Models;

/// <summary>
/// Base search DTO for pagination and filtering
/// </summary>
public class SearchDto
{
    public string? SearchTerm { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? OrderBy { get; set; }
    public OrderDirection OrderDirection { get; set; } = OrderDirection.Descending;
}

public enum OrderDirection
{
    Ascending,
    Descending
}
