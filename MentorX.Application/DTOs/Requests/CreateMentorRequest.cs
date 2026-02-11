namespace MentorX.Application.DTOs.Requests;

public class CreateMentorRequest
{
    public string Name { get; set; } = string.Empty;
    public string PublicBio { get; set; } = string.Empty;
    public string ExpertisePrompt { get; set; } = string.Empty;
    public List<string> ExpertiseTags { get; set; } = new();
}
