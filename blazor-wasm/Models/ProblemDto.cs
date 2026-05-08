namespace blazor_wasm.Models;

/// <summary>
/// DTO for Problem entity
/// </summary>
public class CoordinateDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class ProblemDto
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Guid> EventIds { get; set; } = new List<Guid>();
    public List<CoordinateDto> Coordinates { get; set; } = [];
    public Guid? UserId { get; set; }
    public string? PublicPassword { get; set; }
    public bool HasPublicPassword { get; set; }
}

/// <summary>
/// Search DTO for Problem entity
/// </summary>
public class ProblemSearchDto : SearchDto
{
    // Add any additional search-specific properties here if needed
}
