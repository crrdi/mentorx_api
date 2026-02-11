namespace MentorX.Application.DTOs.Responses;

public class InsightResponse
{
    public Guid Id { get; set; }
    public Guid MentorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Quote { get; set; }
    public List<string> Tags { get; set; } = new();
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool HasMedia { get; set; }
    public string? MediaUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string Type { get; set; } = "insight";
    public Guid? MasterclassPostId { get; set; }
    public MentorSummaryResponse? Mentor { get; set; }
    public bool? IsLiked { get; set; }
}

public class MentorSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int Level { get; set; }
}
