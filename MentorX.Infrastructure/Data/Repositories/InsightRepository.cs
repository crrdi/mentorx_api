using Microsoft.EntityFrameworkCore;
using MentorX.Domain.Entities;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.DbContext;
using Microsoft.Extensions.Logging;

namespace MentorX.Infrastructure.Data.Repositories;

public class InsightRepository : Repository<Insight>, IInsightRepository
{
    private static IQueryable<Insight> WithTagIncludes(IQueryable<Insight> q) =>
        q.Include(i => i.InsightTags).ThenInclude(it => it.Tag)
         .Include(i => i.Mentor).ThenInclude(m => m.Role);

    private readonly ILogger<InsightRepository> _logger;

    public InsightRepository(MentorXDbContext context, ILogger<InsightRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public override async Task<Insight?> GetByIdAsync(Guid id)
    {
        return await WithTagIncludes(_dbSet).FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Insight>> GetByMentorIdAsync(Guid mentorId, int limit, int offset)
    {
        return await WithTagIncludes(_dbSet)
            .Where(i => i.MentorId == mentorId)
            .OrderByDescending(i => i.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Insight>> GetByTagAsync(string tag, string sort, int limit, int offset)
    {
        _logger.LogInformation("[GetByTagAsync] Called with tag: {Tag}, sort: {Sort}, limit: {Limit}, offset: {Offset}", tag, sort, limit, offset);
        
        var query = WithTagIncludes(_dbSet);
        
        // Check if tag is a GUID (tag ID) or a name
        if (Guid.TryParse(tag, out var tagId))
        {
            _logger.LogInformation("[GetByTagAsync] Tag is GUID: {TagId}, searching by TagId", tagId);
            // Tag is a GUID, search by TagId
            query = query.Where(i => i.InsightTags.Any(it => it.TagId == tagId));
        }
        else
        {
            _logger.LogInformation("[GetByTagAsync] Tag is name: {TagName}, searching by Tag.Name", tag);
            // Tag is a name, search by Tag.Name (exact match)
            query = query.Where(i => i.InsightTags.Any(it => it.Tag.Name == tag));
        }
        
        // Apply sorting
        if (sort == "popular")
        {
            query = query.OrderByDescending(i => i.LikeCount);
            _logger.LogInformation("[GetByTagAsync] Sorting by LikeCount DESC (popular)");
        }
        else
        {
            query = query.OrderByDescending(i => i.CreatedAt);
            _logger.LogInformation("[GetByTagAsync] Sorting by CreatedAt DESC (latest)");
        }
        
        var results = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
        
        _logger.LogInformation("[GetByTagAsync] Found {Count} insights", results.Count);
        
        if (results.Any())
        {
            var first = results.First();
            _logger.LogInformation("[GetByTagAsync] First insight ID: {InsightId}, LikeCount: {LikeCount}, CreatedAt: {CreatedAt}, InsightTags count: {TagCount}",
                first.Id, first.LikeCount, first.CreatedAt, first.InsightTags?.Count ?? 0);
        }
        
        return results;
    }

    public async Task<int> GetCountByTagAsync(string tag)
    {
        var query = _dbSet.AsQueryable();
        
        // Check if tag is a GUID (tag ID) or a name
        if (Guid.TryParse(tag, out var tagId))
        {
            query = query.Where(i => i.InsightTags.Any(it => it.TagId == tagId));
        }
        else
        {
            // Tag is a name, search by Tag.Name (exact match)
            query = query.Where(i => i.InsightTags.Any(it => it.Tag.Name == tag));
        }
        
        return await query.CountAsync();
    }

    public async Task<IEnumerable<Insight>> GetByMentorIdsAsync(List<Guid> mentorIds, int limit, int offset, string? tag = null)
    {
        var query = WithTagIncludes(_dbSet).Where(i => mentorIds.Contains(i.MentorId));

        if (!string.IsNullOrEmpty(tag))
        {
            // Check if tag is a GUID (tag ID) or a name
            if (Guid.TryParse(tag, out var tagId))
            {
                query = query.Where(i => i.InsightTags.Any(it => it.TagId == tagId));
            }
            else
            {
                // Tag is a name, search by Tag.Name (exact match)
                query = query.Where(i => i.InsightTags.Any(it => it.Tag.Name == tag));
            }
        }

        return await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetCountByMentorIdsAsync(List<Guid> mentorIds, string? tag = null)
    {
        var query = _dbSet.Where(i => mentorIds.Contains(i.MentorId));

        if (!string.IsNullOrEmpty(tag))
        {
            // Check if tag is a GUID (tag ID) or a name
            if (Guid.TryParse(tag, out var tagId))
            {
                query = query.Where(i => i.InsightTags.Any(it => it.TagId == tagId));
            }
            else
            {
                // Tag is a name, search by Tag.Name (exact match)
                query = query.Where(i => i.InsightTags.Any(it => it.Tag.Name == tag));
            }
        }
        
        return await query.CountAsync();
    }

    public async Task<IEnumerable<Insight>> GetLatestAsync(int limit, int offset)
    {
        return await WithTagIncludes(_dbSet)
            .OrderByDescending(i => i.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Insight>> GetPopularAsync(int limit, int offset)
    {
        return await WithTagIncludes(_dbSet)
            .OrderByDescending(i => i.LikeCount)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task IncrementLikeCountAsync(Guid insightId)
    {
        var insight = await _dbSet.FindAsync(insightId);
        if (insight != null)
        {
            insight.LikeCount++;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DecrementLikeCountAsync(Guid insightId)
    {
        var insight = await _dbSet.FindAsync(insightId);
        if (insight != null)
        {
            insight.LikeCount = Math.Max(0, insight.LikeCount - 1);
            await _context.SaveChangesAsync();
        }
    }

    public async Task IncrementCommentCountAsync(Guid insightId)
    {
        var insight = await _dbSet.FindAsync(insightId);
        if (insight != null)
        {
            insight.CommentCount++;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DecrementCommentCountAsync(Guid insightId)
    {
        var insight = await _dbSet.FindAsync(insightId);
        if (insight != null)
        {
            insight.CommentCount = Math.Max(0, insight.CommentCount - 1);
            await _context.SaveChangesAsync();
        }
    }
}
