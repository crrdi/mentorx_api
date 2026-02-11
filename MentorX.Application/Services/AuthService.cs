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
            throw new ArgumentException("IdToken is required");
        }

        // Supabase ile Google ID token ile giriş yap
        var authResponse = await _supabaseAuthService.SignInWithIdTokenAsync(
            "google",
            request.IdToken,
            request.AccessToken
        );

        if (authResponse.User == null)
        {
            throw new UnauthorizedAccessException("Failed to authenticate with Google");
        }

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
            // Apple'dan gelen email bilgisini kullan (Apple email'i gizleyebilir)
            user = new Domain.Entities.User
            {
                Id = userId,
                Email = authResponse.User.Email ?? string.Empty,
                Name = ExtractNameFromAppleToken(request.IdToken) ?? "User",
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

    private string? ExtractNameFromAppleToken(string idToken)
    {
        try
        {
            // Apple ID token'dan name bilgisini çıkar
            // Apple genellikle name bilgisini ilk login'de gönderir
            // Bu durumda Supabase Auth'dan gelen user metadata'sını kontrol etmek daha iyi olabilir
            // Şimdilik null döndürüyoruz, Supabase Auth otomatik olarak handle edecek
            return null;
        }
        catch
        {
            return null;
        }
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
