namespace MentorX.Application.DTOs.Requests;

public class CreateInsightRequest
{
    public Guid MentorId { get; set; }
    public string? Quote { get; set; }
    public List<string>? Tags { get; set; }
    public bool HasMedia { get; set; } = false;
    public string? MediaUrl { get; set; }
}
