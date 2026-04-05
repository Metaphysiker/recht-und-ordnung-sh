namespace blazor_wasm.Models;

/// <summary>
/// DTO for Problem entity
/// </summary>
public class ProblemDto
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Guid> EventIds { get; set; } = new List<Guid>();
    public Guid? UserId { get; set; }
}

/// <summary>
/// Search DTO for Problem entity
/// </summary>
public class ProblemSearchDto : SearchDto
{
    // Add any additional search-specific properties here if needed
}
