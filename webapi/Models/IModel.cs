namespace webapi.Models;

public interface IModel
{
    Guid Id { get; set; }
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}
