using MentorX.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Supabase;

namespace MentorX.Infrastructure.Services;

public class SupabaseAuthService : ISupabaseAuthService
{
    private readonly SupabaseService _supabaseService;
    private readonly ILogger<SupabaseAuthService> _logger;

    public SupabaseAuthService(SupabaseService supabaseService, ILogger<SupabaseAuthService> logger)
    {
        _supabaseService = supabaseService;
        _logger = logger;
    }

    public async Task<SupabaseAuthResult> SignInWithIdTokenAsync(string provider, string idToken, string? accessToken = null)
    {
        try
        {
            _logger.LogInformation("Attempting to sign in with {Provider} using ID token", provider);
            
            // Supabase C# SDK SignInWithIdToken - Constants.Provider enum kullanılıyor
            var providerEnum = provider.ToLower() switch
            {
                "google" => Supabase.Gotrue.Constants.Provider.Google,
                "apple" => Supabase.Gotrue.Constants.Provider.Apple,
                _ => throw new ArgumentException($"Unsupported provider: {provider}. Supported providers: google, apple")
            };

            if (string.IsNullOrWhiteSpace(idToken))
            {
                _logger.LogError("ID token is null or empty for provider {Provider}", provider);
                throw new ArgumentException("ID token cannot be null or empty");
            }

            _logger.LogDebug("Calling Supabase SignInWithIdToken for provider {Provider}", provider);
            var session = await _supabaseService.Client.Auth.SignInWithIdToken(providerEnum, idToken, accessToken);
            
            if (session == null)
            {
                _logger.LogError("Supabase SignInWithIdToken returned null session for provider {Provider}", provider);
                throw new UnauthorizedAccessException($"Failed to authenticate with {provider}: Session is null");
            }
            
            _logger.LogInformation("Successfully obtained session from Supabase for provider {Provider}", provider);
            
            // Session'ı yükle ve güncelle
            _supabaseService.Client.Auth.LoadSession();
            var updatedSession = await _supabaseService.Client.Auth.RetrieveSessionAsync();

            SupabaseUser? userModel = null;

            // Önce CurrentUser'ı kontrol et
            var currentUser = _supabaseService.Client.Auth.CurrentUser;
            if (currentUser != null)
            {
                _logger.LogDebug("Got user from Supabase CurrentUser: {UserId}", currentUser.Id);
                userModel = new SupabaseUser
                {
                    Id = currentUser.Id ?? string.Empty,
                    Email = currentUser.Email
                };
            }
            else
            {
                _logger.LogDebug("CurrentUser is null, attempting to extract user info from JWT");
            }

            // CurrentUser null ise JWT'den user bilgilerini çıkar
            if (userModel == null)
            {
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

                                _logger.LogDebug("Extracted user info from JWT: {UserId}", id);
                                userModel = new SupabaseUser
                                {
                                    Id = id,
                                    Email = emailObj?.ToString()
                                };
                            }
                        }
                    }
                    catch (Exception jwtEx)
                    {
                        _logger.LogWarning(jwtEx, "Failed to extract user info from JWT token");
                        // JWT parse edilemezse userModel null kalır; üst katman bunu auth hatası olarak görecek
                    }
                }
            }

            if (userModel == null)
            {
                _logger.LogError("Unable to extract user information from Supabase session for provider {Provider}", provider);
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for {Provider} sign in: {Message}", provider, ex.Message);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access for {Provider} sign in: {Message}", provider, ex.Message);
            throw;
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            _logger.LogError(ex, "Supabase Gotrue error for {Provider} sign in. StatusCode: {StatusCode}, Message: {Message}, Response: {Response}", 
                provider, ex.StatusCode, ex.Message, ex.ResponseContent);
            
            // Supabase'in döndüğü hata mesajını daha anlaşılır hale getir
            var errorMessage = ex.Message;
            if (!string.IsNullOrEmpty(ex.ResponseContent))
            {
                try
                {
                    // JSON response'dan error mesajını çıkarmaya çalış
                    var responseLower = ex.ResponseContent.ToLower();
                    if (responseLower.Contains("invalid") || responseLower.Contains("token"))
                    {
                        errorMessage = "Geçersiz Google token. Lütfen tekrar giriş yapmayı deneyin.";
                    }
                    else if (responseLower.Contains("provider") || responseLower.Contains("oauth"))
                    {
                        errorMessage = "Google OAuth yapılandırması eksik veya hatalı. Lütfen sistem yöneticisine başvurun.";
                    }
                }
                catch
                {
                    // JSON parse edilemezse orijinal mesajı kullan
                }
            }
            
            throw new UnauthorizedAccessException(errorMessage, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during {Provider} sign in: {Message}", provider, ex.Message);
            throw new Exception($"Google ile giriş yapılırken bir hata oluştu: {ex.Message}", ex);
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
