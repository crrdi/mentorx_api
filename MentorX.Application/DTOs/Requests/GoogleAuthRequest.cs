namespace MentorX.Application.DTOs.Requests;

public class GoogleAuthRequest
{
    public string IdToken { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
}
