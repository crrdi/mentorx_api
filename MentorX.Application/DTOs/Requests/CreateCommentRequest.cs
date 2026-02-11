namespace MentorX.Application.DTOs.Requests;

public class CreateCommentRequest
{
    public string? Content { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? MentorId { get; set; }
}
