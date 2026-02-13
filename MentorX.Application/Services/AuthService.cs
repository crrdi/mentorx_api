using AutoMapper;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;
using MentorX.Application.Interfaces;
using MentorX.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MentorX.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISupabaseAuthService _supabaseAuthService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork unitOfWork,
        ISupabaseAuthService supabaseAuthService,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _supabaseAuthService = supabaseAuthService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AuthResponse> GoogleAuthAsync(GoogleAuthRequest request)
    {
        if (string.IsNullOrEmpty(request.IdToken))
        {
            _logger.LogWarning("Google auth request received with empty IdToken");
            throw new ArgumentException("IdToken is required");
        }

        _logger.LogInformation("Processing Google authentication request");

        // Supabase ile Google ID token ile giriş yap
        var authResponse = await _supabaseAuthService.SignInWithIdTokenAsync(
            "google",
            request.IdToken,
            request.AccessToken
        );

        if (authResponse.User == null)
        {
            _logger.LogWarning("Supabase authentication succeeded but user is null");
            throw new UnauthorizedAccessException("Google sign-in failed: Unable to retrieve user information. Please try again.");
        }

        _logger.LogInformation("Google authentication successful for user {UserId}", authResponse.User.Id);

        var userId = Guid.Parse(authResponse.User.Id);
        
        // Kullanıcı profilini kontrol et veya oluştur
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        
        if (user == null)
        {
            // Yeni kullanıcı - profil oluştur
            // Supabase Auth'dan gelen email ve name bilgilerini kullan
            user = new Domain.Entities.User
            {
                Id = userId,
                Email = authResponse.User.Email ?? string.Empty,
                Name = ExtractNameFromToken(request.IdToken) ?? "User",
                Credits = 10,
                FocusAreas = new List<string>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user);

            // Actor oluştur
            var actor = new Domain.Entities.Actor
            {
                Id = Guid.NewGuid(),
                Type = Domain.Enums.ActorType.User,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Actors.AddAsync(actor);
            await _unitOfWork.SaveChangesAsync();
        }

        return new AuthResponse
        {
            User = _mapper.Map<UserResponse>(user),
            Session = new SessionResponse
            {
                AccessToken = authResponse.Session?.AccessToken ?? string.Empty,
                RefreshToken = authResponse.Session?.RefreshToken ?? string.Empty,
                ExpiresIn = authResponse.Session?.ExpiresIn ?? 3600,
                TokenType = "bearer",
                User = new SessionUserResponse
                {
                    Id = user.Id,
                    Email = user.Email
                }
            }
        };
    }

    public async Task<AuthResponse> AppleAuthAsync(AppleAuthRequest request)
    {
        if (string.IsNullOrEmpty(request.IdToken))
        {
            throw new ArgumentException("IdToken is required");
        }

        // Supabase ile Apple ID token ile giriş yap
        var authResponse = await _supabaseAuthService.SignInWithIdTokenAsync(
            "apple",
            request.IdToken,
            request.AccessToken
        );

        if (authResponse.User == null)
        {
            throw new UnauthorizedAccessException("Failed to authenticate with Apple");
        }

        var userId = Guid.Parse(authResponse.User.Id);
        
        // Kullanıcı profilini kontrol et veya oluştur
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        
        if (user == null)
        {
            // Yeni kullanıcı - profil oluştur
            // Apple name sadece ilk girişte client'tan gelir (request.FullName)
            user = new Domain.Entities.User
            {
                Id = userId,
                Email = authResponse.User.Email ?? string.Empty,
                Name = GetAppleUserName(request),
                Credits = 10,
                FocusAreas = new List<string>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user);

            // Actor oluştur
            var actor = new Domain.Entities.Actor
            {
                Id = Guid.NewGuid(),
                Type = Domain.Enums.ActorType.User,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Actors.AddAsync(actor);
            await _unitOfWork.SaveChangesAsync();
        }

        return new AuthResponse
        {
            User = _mapper.Map<UserResponse>(user),
            Session = new SessionResponse
            {
                AccessToken = authResponse.Session?.AccessToken ?? string.Empty,
                RefreshToken = authResponse.Session?.RefreshToken ?? string.Empty,
                ExpiresIn = authResponse.Session?.ExpiresIn ?? 3600,
                TokenType = "bearer",
                User = new SessionUserResponse
                {
                    Id = user.Id,
                    Email = user.Email
                }
            }
        };
    }

    public async Task<AuthResponse> EmailLoginAsync(LoginRequest request)
    {
        // Hardcoded test credentials - sadece bu email/şifre çalışır
        if (request.Email != "erdiacar@gmail.com" || request.Password != "Sandbox123456")
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // DB'den kullanıcıyı email ile bul
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        // Basit JWT token oluştur (auth handler sadece payload decode eder, imza kontrol etmez)
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        var exp = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            $"{{\"sub\":\"{user.Id}\",\"email\":\"{user.Email}\",\"iat\":{iat},\"exp\":{exp}}}"));
        var token = $"{header}.{payload}.test-signature";

        return new AuthResponse
        {
            User = _mapper.Map<UserResponse>(user),
            Session = new SessionResponse
            {
                AccessToken = token,
                RefreshToken = token,
                ExpiresIn = 86400,
                TokenType = "bearer",
                User = new SessionUserResponse
                {
                    Id = user.Id,
                    Email = user.Email
                }
            }
        };
    }

    private string? ExtractNameFromToken(string idToken)
    {
        try
        {
            // Google ID token'dan name bilgisini çıkar
            // JWT token'ı decode et ve "name" claim'ini al
            var parts = idToken.Split('.');
            if (parts.Length < 2) return null;

            var payload = parts[1];
            // Base64 padding ekle
            var padding = payload.Length % 4;
            if (padding > 0)
            {
                payload += new string('=', 4 - padding);
            }

            var jsonBytes = Convert.FromBase64String(payload);
            var json = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                System.Text.Encoding.UTF8.GetString(jsonBytes));

            if (json != null && json.TryGetValue("name", out var nameObj))
            {
                return nameObj?.ToString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Apple name is only provided by the client on first sign-in (from credential.fullName).
    /// Apple does not include name in the ID token.
    /// </summary>
    private static string GetAppleUserName(AppleAuthRequest request)
    {
        var name = request.FullName?.Trim();
        return !string.IsNullOrEmpty(name) ? name : "User";
    }

    public async Task<UserResponse?> GetCurrentUserAsync(string accessToken)
    {
        var user = await _supabaseAuthService.GetUserAsync(accessToken);
        if (user == null)
        {
            return null;
        }

        var userId = Guid.Parse(user.Id);
        var userEntity = await _unitOfWork.Users.GetByIdAsync(userId);
        
        return userEntity != null ? _mapper.Map<UserResponse>(userEntity) : null;
    }
}
