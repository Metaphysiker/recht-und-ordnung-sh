namespace webapi.Models.DTOs;

/// <summary>
/// Paginated response wrapper
/// </summary>
/// <typeparam name="T">The type of items in the page</typeparam>
public class PaginationDto<T>
{
    /// <summary>
    /// The items in the current page
    /// </summary>
    public required IEnumerable<T> Items { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public required int Total { get; set; }

    /// <summary>
    /// Current page number (0-based)
    /// </summary>
    public required int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public required int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages - 1;

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 0;
}
