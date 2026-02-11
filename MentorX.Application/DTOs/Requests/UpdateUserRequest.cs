namespace MentorX.Application.DTOs.Requests;

public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public List<string>? FocusAreas { get; set; }
    public string? Avatar { get; set; }
}
