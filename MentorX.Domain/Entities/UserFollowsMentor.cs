namespace MentorX.Domain.Entities;

public class UserFollowsMentor
{
    public Guid UserId { get; set; }
    public Guid MentorId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Mentor Mentor { get; set; } = null!;
}
