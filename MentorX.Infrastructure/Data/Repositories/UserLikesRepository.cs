using Microsoft.EntityFrameworkCore;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.DbContext;

namespace MentorX.Infrastructure.Data.Repositories;

public class UserLikesRepository : IUserLikesRepository
{
    private readonly MentorXDbContext _context;

    public UserLikesRepository(MentorXDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsLikedAsync(Guid userId, Guid insightId)
    {
        return await _context.UserLikes
            .AnyAsync(ul => ul.UserId == userId && ul.InsightId == insightId);
    }

    public async Task LikeAsync(Guid userId, Guid insightId)
    {
        var exists = await IsLikedAsync(userId, insightId);
        if (!exists)
        {
            _context.UserLikes.Add(new Domain.Entities.UserLikes
            {
                UserId = userId,
                InsightId = insightId,
                CreatedAt = DateTime.UtcNow
            });
            
            // Increment like count
            var insight = await _context.Insights.FindAsync(insightId);
            if (insight != null)
            {
                insight.LikeCount++;
            }
            
            await _context.SaveChangesAsync();
        }
    }

    public async Task UnlikeAsync(Guid userId, Guid insightId)
    {
        var like = await _context.UserLikes
            .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.InsightId == insightId);
        
        if (like != null)
        {
            _context.UserLikes.Remove(like);
            
            // Decrement like count
            var insight = await _context.Insights.FindAsync(insightId);
            if (insight != null)
            {
                insight.LikeCount = Math.Max(0, insight.LikeCount - 1);
            }
            
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Guid>> GetLikedInsightIdsAsync(Guid userId)
    {
        return await _context.UserLikes
            .Where(ul => ul.UserId == userId)
            .Select(ul => ul.InsightId)
            .ToListAsync();
    }
}
