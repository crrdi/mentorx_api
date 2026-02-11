using MentorX.Domain.Entities;

namespace MentorX.Domain.Interfaces;

public interface ICommentRepository : IRepository<Comment>
{
    Task<IEnumerable<Comment>> GetByInsightIdAsync(Guid insightId);
    Task<IEnumerable<Comment>> GetTopLevelByInsightIdAsync(Guid insightId, int limit, int offset);
    Task<int> GetTopLevelCountByInsightIdAsync(Guid insightId);
    Task<IEnumerable<Comment>> GetByAuthorActorIdAsync(Guid authorActorId);
}
