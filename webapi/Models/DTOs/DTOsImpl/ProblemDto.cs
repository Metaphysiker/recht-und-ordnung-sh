namespace webapi.Models.DTOs.DTOsImpl;

/// <summary>
/// DTO for Problem entity
/// </summary>
public class ProblemDto : IDto
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Collection of related Event IDs
    public List<Guid> EventIds { get; set; } = new List<Guid>();
    
    // User relationship
    public Guid? UserId { get; set; }

    string IDto.GenericName { get; set; } = string.Empty;
}
