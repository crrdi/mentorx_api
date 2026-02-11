using Microsoft.EntityFrameworkCore;
using MentorX.Domain.Entities;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.DbContext;

namespace MentorX.Infrastructure.Data.Repositories;

public class CommentRepository : Repository<Comment>, ICommentRepository
{
    public CommentRepository(MentorXDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Comment>> GetByInsightIdAsync(Guid insightId)
    {
        return await _dbSet
            .Where(c => c.InsightId == insightId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Comment>> GetTopLevelByInsightIdAsync(Guid insightId, int limit, int offset)
    {
        return await _dbSet
            .Where(c => c.InsightId == insightId && c.ParentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetTopLevelCountByInsightIdAsync(Guid insightId)
    {
        return await _dbSet
            .CountAsync(c => c.InsightId == insightId && c.ParentId == null);
    }

    public async Task<IEnumerable<Comment>> GetByAuthorActorIdAsync(Guid authorActorId)
    {
        return await _dbSet
            .Where(c => c.AuthorActorId == authorActorId && c.ParentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
}
