namespace webapi.Search;

/// <summary>
/// Search DTO for Event entity
/// </summary>
public class EventSearchDto : Search
{
    public Guid? ProblemId { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
}
