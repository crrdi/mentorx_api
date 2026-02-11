namespace MentorX.Domain.Entities;

public class UserLikes
{
    public Guid UserId { get; set; }
    public Guid InsightId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Insight Insight { get; set; } = null!;
}
