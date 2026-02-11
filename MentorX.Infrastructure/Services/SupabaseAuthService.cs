using MentorX.Application.Interfaces;
using Supabase;

namespace MentorX.Infrastructure.Services;

public class SupabaseAuthService : ISupabaseAuthService
{
    private readonly SupabaseService _supabaseService;

    public SupabaseAuthService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<SupabaseAuthResult> SignInWithIdTokenAsync(string provider, string idToken, string? accessToken = null)
    {
        try
        {
            // Supabase C# SDK SignInWithIdToken - Constants.Provider enum kullanılıyor
            var providerEnum = provider.ToLower() switch
            {
                "google" => Supabase.Gotrue.Constants.Provider.Google,
                "apple" => Supabase.Gotrue.Constants.Provider.Apple,
                _ => throw new ArgumentException($"Unsupported provider: {provider}. Supported providers: google, apple")
            };

            var session = await _supabaseService.Client.Auth.SignInWithIdToken(providerEnum, idToken, accessToken);
            
            // Session'ı yükle ve güncelle
            _supabaseService.Client.Auth.LoadSession();
            var updatedSession = await _supabaseService.Client.Auth.RetrieveSessionAsync();

            SupabaseUser? userModel = null;

            // Bazı durumlarda CurrentUser null dönebiliyor, bu yüzden JWT'den user bilgilerini çıkarıyoruz
            var accessTokenValue = updatedSession?.AccessToken ?? session?.AccessToken;
            if (!string.IsNullOrEmpty(accessTokenValue))
            {
                try
                {
                    var parts = accessTokenValue.Split('.');
                    if (parts.Length >= 2)
                    {
                        var payload = parts[1];
                        var padding = payload.Length % 4;
                        if (padding > 0)
                        {
                            payload += new string('=', 4 - padding);
                        }

                        var jsonBytes = Convert.FromBase64String(payload);
                        var json = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                            System.Text.Encoding.UTF8.GetString(jsonBytes));

                        if (json != null && json.TryGetValue("sub", out var subObj))
                        {
                            var id = subObj?.ToString() ?? string.Empty;
                            json.TryGetValue("email", out var emailObj);

                            userModel = new SupabaseUser
                            {
                                Id = id,
                                Email = emailObj?.ToString()
                            };
                        }
                    }
                }
                catch
                {
                    // JWT parse edilemezse userModel null kalır; üst katman bunu auth hatası olarak görecek
                }
            }
            
            return new SupabaseAuthResult
            {
                User = userModel,
                Session = updatedSession != null ? new SupabaseSession
                {
                    AccessToken = updatedSession.AccessToken ?? string.Empty,
                    RefreshToken = updatedSession.RefreshToken ?? string.Empty,
                    ExpiresIn = updatedSession.ExpiresIn > 0 ? (int)updatedSession.ExpiresIn : 3600
                } : session != null ? new SupabaseSession
                {
                    AccessToken = session.AccessToken ?? string.Empty,
                    RefreshToken = session.RefreshToken ?? string.Empty,
                    ExpiresIn = session.ExpiresIn > 0 ? (int)session.ExpiresIn : 3600
                } : null
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to sign in with {provider}: {ex.Message}", ex);
        }
    }

    public async Task<SupabaseUser?> GetUserAsync(string accessToken)
    {
        try
        {
            // Set session first to authenticate
            await _supabaseService.Client.Auth.SetSession(accessToken, string.Empty);
            var user = _supabaseService.Client.Auth.CurrentUser;
            
            return user != null ? new SupabaseUser
            {
                Id = user.Id ?? string.Empty,
                Email = user.Email ?? string.Empty
            } : null;
        }
        catch
        {
            return null;
        }
    }
}
