using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;

namespace MentorX.Application.Interfaces;

public interface IMentorService
{
    Task<PagedResponse<MentorResponse>> GetMentorsAsync(string? tag, bool popular, bool followed, string? search, int limit, int offset, Guid? userId);
    Task<MentorResponse?> GetMentorByIdAsync(Guid id, Guid? userId);
    Task<MentorResponse> CreateMentorAsync(Guid userId, CreateMentorRequest request);
    Task<MentorResponse> UpdateMentorAsync(Guid mentorId, Guid userId, UpdateMentorRequest request);
    Task<SuccessResponse> FollowMentorAsync(Guid mentorId, Guid userId);
    Task<SuccessResponse> UnfollowMentorAsync(Guid mentorId, Guid userId);
    Task<MentorRepliesResponse> GetMentorRepliesAsync(Guid mentorId);
}
