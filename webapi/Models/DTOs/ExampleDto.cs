using webapi.Models;

namespace webapi.Models.DTOs;

/// <summary>
/// Example DTO - Create DTOs like this for your entities
/// </summary>
public class ExampleDto : IDto
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    string IDto.GenericName { get; set; } = string.Empty;
}
