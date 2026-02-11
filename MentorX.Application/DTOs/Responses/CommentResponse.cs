namespace MentorX.Application.DTOs.Responses;

public class CommentResponse
{
    public Guid Id { get; set; }
    public Guid InsightId { get; set; }
    public Guid AuthorActorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? ParentId { get; set; }
    public AuthorResponse Author { get; set; } = null!;
}

public class AuthorResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
