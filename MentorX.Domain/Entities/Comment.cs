namespace MentorX.Domain.Entities;

public class Comment : BaseEntity
{
    public Guid InsightId { get; set; }
    public Guid AuthorActorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int LikeCount { get; set; } = 0;
    public DateTime? EditedAt { get; set; }
    public bool IsEdited { get; set; } = false;
    public Guid? ParentId { get; set; }
    
    // Navigation properties
    public Insight Insight { get; set; } = null!;
    public Actor AuthorActor { get; set; } = null!;
    public Comment? Parent { get; set; }
    public List<Comment> Replies { get; set; } = new();
}
