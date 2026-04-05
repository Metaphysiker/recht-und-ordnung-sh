using webapi.Models.ModelsImpl;

namespace webapi.Models;

public interface IEntityWithUser
{
    ApplicationUser? User { get; set; }
    Guid? UserId { get; set; }
}
