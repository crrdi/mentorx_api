namespace MentorX.Domain.Entities;

public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid SenderActorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime? EditedAt { get; set; }
    public bool IsEdited { get; set; } = false;
    
    // Navigation properties
    public Conversation Conversation { get; set; } = null!;
    public Actor SenderActor { get; set; } = null!;
}
