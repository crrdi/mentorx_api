namespace MentorX.Application.DTOs.Responses;

public class SendMessageResponse
{
    public MessageResponse UserMessage { get; set; } = null!;
    public MessageResponse? MentorReply { get; set; }
}
