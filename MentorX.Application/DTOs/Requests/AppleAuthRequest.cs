namespace MentorX.Application.DTOs.Requests;

public class AppleAuthRequest
{
    public string IdToken { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
}
