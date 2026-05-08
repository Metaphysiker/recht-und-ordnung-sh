namespace webapi.Models.DTOs.DTOsImpl;

/// <summary>
/// DTO for Problem entity
/// </summary>
public class ProblemDto : IDto
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<Guid> EventIds { get; set; } = new List<Guid>();
    public List<CoordinateDto> Coordinates { get; set; } = [];
    public Guid? UserId { get; set; }
    public string? PublicPassword { get; set; }
    public bool HasPublicPassword { get; set; }
    public string? Email { get; set; }

    string IDto.GenericName { get; set; } = string.Empty;
}
