namespace webapi.Models.DTOs.DTOsImpl;

/// <summary>
/// DTO for Event entity
/// </summary>
public class EventDto : IDto
{
    public Guid? Id { get; set; }
    public DateTimeOffset EventDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Optional relationship to Problem
    public Guid? ProblemId { get; set; }
    
    // User relationship
    public Guid? UserId { get; set; }

    string IDto.GenericName { get; set; } = string.Empty;
}
