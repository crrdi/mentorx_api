namespace MentorX.Application.DTOs.Responses;

public class TagResponse
{
    public string Tag { get; set; } = string.Empty;
    public int MentorCount { get; set; }
    public int PostCount { get; set; }
}
