using MentorX.Domain.Enums;

namespace MentorX.Domain.Entities;

public class Actor : BaseEntity
{
    public ActorType Type { get; set; }
    public Guid? UserId { get; set; }
    public Guid? MentorId { get; set; }
    
    // Navigation properties
    public User? User { get; set; }
    public Mentor? Mentor { get; set; }
    public List<Comment> Comments { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
}
