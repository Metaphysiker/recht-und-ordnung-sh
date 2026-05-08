namespace webapi.Search;

public class AttachmentSearchDto : Search
{
    public Guid? ProblemId { get; set; }
    public Guid? EventId { get; set; }
}
