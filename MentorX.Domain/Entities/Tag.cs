namespace MentorX.Domain.Entities;

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    // Navigation
    public List<MentorTag> MentorTags { get; set; } = new();
    public List<InsightTag> InsightTags { get; set; } = new();
}
