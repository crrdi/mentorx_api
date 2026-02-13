using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;

namespace MentorX.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> GoogleAuthAsync(GoogleAuthRequest request);
    Task<AuthResponse> AppleAuthAsync(AppleAuthRequest request);
    Task<AuthResponse> EmailLoginAsync(LoginRequest request);
    Task<UserResponse?> GetCurrentUserAsync(string accessToken);
}
