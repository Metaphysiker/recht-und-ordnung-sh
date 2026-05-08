namespace blazor_wasm.Models;

public class AttachmentDto
{
    public Guid? Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string S3Key { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public Guid? ProblemId { get; set; }
    public Guid? EventId { get; set; }
    public Guid? UserId { get; set; }
}

public class AttachmentSearchDto : SearchDto
{
    public Guid? ProblemId { get; set; }
    public Guid? EventId { get; set; }
}
