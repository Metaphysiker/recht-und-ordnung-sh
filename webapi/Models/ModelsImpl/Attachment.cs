namespace webapi.Models.ModelsImpl;

public class Attachment : IModel, IEntityWithUser
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string S3Key { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public Guid? ProblemId { get; set; }
    public Problem? Problem { get; set; }
    public ApplicationUser? User { get; set; }
    public Guid? UserId { get; set; }
}
