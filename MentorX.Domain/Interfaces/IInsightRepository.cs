using MentorX.Domain.Entities;

namespace MentorX.Domain.Interfaces;

public interface IInsightRepository : IRepository<Insight>
{
    Task<IEnumerable<Insight>> GetByMentorIdAsync(Guid mentorId, int limit, int offset);
    Task<IEnumerable<Insight>> GetByTagAsync(string tag, string sort, int limit, int offset);
    Task<int> GetCountByTagAsync(string tag);
    Task<IEnumerable<Insight>> GetByMentorIdsAsync(List<Guid> mentorIds, int limit, int offset, string? tag = null);
    Task<int> GetCountByMentorIdsAsync(List<Guid> mentorIds, string? tag = null);
    Task<IEnumerable<Insight>> GetLatestAsync(int limit, int offset);
    Task<IEnumerable<Insight>> GetPopularAsync(int limit, int offset);
    Task IncrementLikeCountAsync(Guid insightId);
    Task DecrementLikeCountAsync(Guid insightId);
    Task IncrementCommentCountAsync(Guid insightId);
    Task DecrementCommentCountAsync(Guid insightId);
}
