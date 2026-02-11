namespace MentorX.Application.DTOs.Responses;

public class AuthResponse
{
    public UserResponse User { get; set; } = null!;
    public SessionResponse Session { get; set; } = null!;
}

public class SessionResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "bearer";
    public SessionUserResponse User { get; set; } = null!;
}

public class SessionUserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
}
