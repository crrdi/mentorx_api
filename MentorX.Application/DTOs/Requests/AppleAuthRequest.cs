namespace MentorX.Application.DTOs.Requests;

public class AppleAuthRequest
{
    public string IdToken { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    /// <summary>
    /// Full name from Apple credential (givenName + familyName). Only provided on first sign-in.
    /// Client should send this when available from Apple's authorization response.
    /// </summary>
    public string? FullName { get; set; }
}
