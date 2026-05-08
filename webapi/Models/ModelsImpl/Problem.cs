
using System.Collections.ObjectModel;

namespace webapi.Models.ModelsImpl;

public class Problem : IModel, IEntityWithUser
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Collection<Event> Events { get; set; } = new Collection<Event>();
    public Collection<Attachment> Attachments { get; set; } = new Collection<Attachment>();
    public List<Coordinate> Coordinates { get; set; } = [];
    public ApplicationUser? User { get; set;}
    public Guid? UserId { get; set; }
}
