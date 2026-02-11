using MentorX.Domain.Enums;

namespace MentorX.Domain.Entities;

public class Mentor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string PublicBio { get; set; } = string.Empty;
    public string ExpertisePrompt { get; set; } = string.Empty; // PRIVATE - Never expose in API
    public int Level { get; set; } = 1;
    public Guid RoleId { get; set; }
    public int FollowerCount { get; set; } = 0;
    public int InsightCount { get; set; } = 0;
    public Guid CreatedBy { get; set; }
    public string? Avatar { get; set; }

    // Navigation properties
    public List<MentorTag> MentorTags { get; set; } = new();
    public MentorRole Role { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public List<Insight> Insights { get; set; } = new();
    public List<UserFollowsMentor> Followers { get; set; } = new();
    public List<Conversation> Conversations { get; set; } = new();
    public Actor? Actor { get; set; }
    public MentorAutomation? Automation { get; set; }
}
