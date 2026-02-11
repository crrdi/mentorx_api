using MentorX.Domain.Enums;

namespace MentorX.Domain.Entities;

public class Insight : BaseEntity
{
    public Guid MentorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Quote { get; set; }
    public int LikeCount { get; set; } = 0;
    public int CommentCount { get; set; } = 0;
    public bool HasMedia { get; set; } = false;
    public string? MediaUrl { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsEdited { get; set; } = false;
    public InsightType Type { get; set; } = InsightType.Insight;
    public Guid? MasterclassPostId { get; set; }
    
    // Navigation properties
    public List<InsightTag> InsightTags { get; set; } = new();
    public Mentor Mentor { get; set; } = null!;
    public List<Comment> Comments { get; set; } = new();
    public List<UserLikes> Likes { get; set; } = new();
}
