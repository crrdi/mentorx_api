using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;

namespace MentorX.Application.Interfaces;

public interface IInsightService
{
    Task<PagedResponse<InsightResponse>> GetInsightsAsync(string? tag, Guid? mentorId, string? mentorIds, string? sort, int limit, int offset, Guid? userId);
    Task<InsightResponse?> GetInsightByIdAsync(Guid id, Guid? userId);
    Task<InsightResponse> CreateInsightAsync(Guid userId, CreateInsightRequest request);
    Task<List<InsightResponse>> CreateThreadAsync(Guid userId, CreateInsightRequest request);
    Task<PagedResponse<InsightResponse>> GetFeedAsync(Guid userId, string? tag, int limit, int offset);
    Task<LikeResponse> LikeInsightAsync(Guid userId, Guid insightId);
    Task<LikeResponse> UnlikeInsightAsync(Guid userId, Guid insightId);
}
