namespace MentorX.Domain.Entities;

public class Conversation : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid MentorId { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public int UserUnreadCount { get; set; } = 0;
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Mentor Mentor { get; set; } = null!;
    public List<Message> Messages { get; set; } = new();
}
