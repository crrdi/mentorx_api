using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;

namespace MentorX.Application.Interfaces;

public interface ICommentService
{
    Task<PagedResponse<CommentResponse>> GetCommentsByInsightIdAsync(Guid insightId, int limit, int offset);
    Task<CommentResponse> CreateCommentAsync(Guid insightId, Guid userId, CreateCommentRequest request);
}
