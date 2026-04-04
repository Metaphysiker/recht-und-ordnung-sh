using Microsoft.AspNetCore.Identity;

namespace webapi.Models.ModelsImpl;

public class ApplicationUser : IdentityUser<Guid>, IModel
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
