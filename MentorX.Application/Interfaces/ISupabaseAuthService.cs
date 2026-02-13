namespace MentorX.Application.Interfaces;

public interface ISupabaseAuthService
{
    Task<SupabaseAuthResult> SignInWithIdTokenAsync(string provider, string idToken, string? accessToken = null);
    Task<SupabaseAuthResult> SignInWithEmailAsync(string email, string password);
    Task<SupabaseUser?> GetUserAsync(string accessToken);
}

public class SupabaseAuthResult
{
    public SupabaseUser? User { get; set; }
    public SupabaseSession? Session { get; set; }
}

public class SupabaseUser
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class SupabaseSession
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}
