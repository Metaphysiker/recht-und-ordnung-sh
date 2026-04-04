namespace webapi.Models.DTOs;

public interface IDto
{
    Guid? Id { get; set; }
    String GenericName { get; set; }
}
