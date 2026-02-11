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

            // Log token info for debugging (first 20 chars only for security)
            var tokenPreview = idToken.Length > 20 ? idToken.Substring(0, 20) + "..." : idToken;
            _logger.LogDebug("Calling Supabase SignInWithIdToken for provider {Provider}. Token preview: {TokenPreview}, Token length: {TokenLength}", 
                provider, tokenPreview, idToken.Length);
            
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
            // Log full error details for debugging
            _logger.LogError(ex, 
                "Supabase Gotrue error for {Provider} sign in. " +
                "StatusCode: {StatusCode}, " +
                "Message: {Message}, " +
                "Exception Type: {ExceptionType}, " +
                "Stack Trace: {StackTrace}",
                provider, 
                ex.StatusCode, 
                ex.Message,
                ex.GetType().Name,
                ex.StackTrace);
            
            // Supabase'in döndüğü hata mesajını daha anlaşılır hale getir
            var errorMessage = ex.Message;
            var errorCode = "GOOGLE_AUTH_ERROR";
            
            if (!string.IsNullOrEmpty(ex.Message))
            {
                try
                {
                    // Hata mesajından error tipini çıkarmaya çalış
                    var messageLower = ex.Message.ToLower();
                    
                    if (messageLower.Contains("invalid") || messageLower.Contains("token") || messageLower.Contains("expired"))
                    {
                        errorMessage = "Google sign-in failed: Invalid or expired token. Please try signing in again.";
                        errorCode = "GOOGLE_TOKEN_INVALID";
                        _logger.LogWarning("Token validation failed. This usually means: 1) Token expired, 2) Token format is incorrect, 3) Token was not issued by Google");
                    }
                    else if (messageLower.Contains("provider") || messageLower.Contains("oauth") || messageLower.Contains("configuration"))
                    {
                        errorMessage = "Google sign-in failed: OAuth configuration is missing or incorrect. Please contact system administrator.";
                        errorCode = "GOOGLE_OAUTH_CONFIG_ERROR";
                        _logger.LogWarning("OAuth configuration error. Check Supabase Dashboard > Authentication > Providers > Google settings");
                    }
                    else if (ex.StatusCode == 401 || ex.StatusCode == 403)
                    {
                        errorMessage = "Google sign-in failed: Authentication error. Please try again.";
                        errorCode = "GOOGLE_AUTH_UNAUTHORIZED";
                        _logger.LogWarning("Unauthorized error (401/403). Check if Google OAuth is properly configured in Supabase");
                    }
                    else
                    {
                        errorMessage = $"Google sign-in failed: {ex.Message}";
                        _logger.LogWarning("Unknown error type. Original message: {Message}", ex.Message);
                    }
                }
                catch (Exception parseEx)
                {
                    _logger.LogError(parseEx, "Failed to parse error message. Using original message.");
                    // Parse edilemezse orijinal mesajı kullan
                }
            }
            
            throw new UnauthorizedAccessException(errorMessage, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during {Provider} sign in: {Message}", provider, ex.Message);
            throw new Exception($"An error occurred while signing in with Google: {ex.Message}", ex);
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
