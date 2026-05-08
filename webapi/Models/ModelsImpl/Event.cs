using System.Collections.ObjectModel;

namespace webapi.Models.ModelsImpl;

public class Event: IModel, IEntityWithUser
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset EventDate { get; set; } = DateTimeOffset.UtcNow;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ProblemId { get; set; }
    public Problem? Problem { get; set; }
    public Collection<Attachment> Attachments { get; set; } = [];
    public ApplicationUser? User { get; set;}
    public Guid? UserId { get; set; }
}
