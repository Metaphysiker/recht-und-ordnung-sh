using webapi.Models;

namespace webapi.Models.ModelsImpl;

/// <summary>
/// Example entity - Create entities like this implementing IModel
/// </summary>
public class ExampleEntity : IModel
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
