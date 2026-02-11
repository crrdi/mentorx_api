using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;

namespace MentorX.Application.Interfaces;

public interface IUserService
{
    Task<UserResponse> GetCurrentUserAsync(Guid userId);
    Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<PagedResponse<MentorResponse>> GetCreatedMentorsAsync(Guid userId, int limit, int offset);
    Task<PagedResponse<MentorResponse>> GetFollowingMentorsAsync(Guid userId, int limit, int offset);
}
