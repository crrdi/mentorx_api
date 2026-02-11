namespace MentorX.Application.DTOs.Responses;

public class MentorRepliesResponse
{
    public List<MentorReplyItem> Replies { get; set; } = new();
}

public class MentorReplyItem
{
    public CommentResponse Comment { get; set; } = null!;
    public InsightResponse ParentPost { get; set; } = null!;
}
