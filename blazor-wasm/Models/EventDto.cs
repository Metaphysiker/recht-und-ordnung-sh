namespace blazor_wasm.Models;

/// <summary>
/// DTO for Event entity
/// </summary>
public class EventDto
{
    public Guid? Id { get; set; }
    public DateTimeOffset EventDate { get; set; } = DateTimeOffset.UtcNow;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ProblemId { get; set; }
    public Guid? UserId { get; set; }
}

/// <summary>
/// Search DTO for Event entity
/// </summary>
public class EventSearchDto : SearchDto
{
    public Guid? ProblemId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
