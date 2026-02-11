namespace MentorX.Application.DTOs.Responses;

public class MentorResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PublicBio { get; set; } = string.Empty;
    public List<string> ExpertiseTags { get; set; } = new();
    public int Level { get; set; }
    public string Role { get; set; } = string.Empty;
    public int FollowerCount { get; set; }
    public int InsightCount { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? Avatar { get; set; }
    public bool? IsFollowing { get; set; }
}
