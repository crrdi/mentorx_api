namespace MentorX.Application.DTOs.Responses;

public class ConversationResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid MentorId { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public int UserUnreadCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public MentorSummaryResponse? Mentor { get; set; }
}
