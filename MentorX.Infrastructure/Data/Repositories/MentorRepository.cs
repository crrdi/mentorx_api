using Microsoft.EntityFrameworkCore;
using MentorX.Domain.Entities;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.DbContext;

namespace MentorX.Infrastructure.Data.Repositories;

public class MentorRepository : Repository<Mentor>, IMentorRepository
{
    private static IQueryable<Mentor> WithTagIncludes(IQueryable<Mentor> q) =>
        q.Include(m => m.MentorTags).ThenInclude(mt => mt.Tag);

    public MentorRepository(MentorXDbContext context) : base(context)
    {
    }

    public override async Task<Mentor?> GetByIdAsync(Guid id)
    {
        return await WithTagIncludes(_dbSet).FirstOrDefaultAsync(m => m.Id == id);
    }

    public override async Task<IEnumerable<Mentor>> GetAllAsync()
    {
        return await WithTagIncludes(_dbSet).ToListAsync();
    }

    public async Task<IEnumerable<Mentor>> GetByTagAsync(string tag)
    {
        var query = WithTagIncludes(_dbSet);
        
        // Check if tag is a GUID (tag ID) or a name
        if (Guid.TryParse(tag, out var tagId))
        {
            // Tag is a GUID, search by TagId
            query = query.Where(m => m.MentorTags.Any(mt => mt.TagId == tagId));
        }
        else
        {
            // Tag is a name, search by Tag.Name
            query = query.Where(m => m.MentorTags.Any(mt => mt.Tag.Name == tag));
        }
        
        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Mentor>> SearchAsync(string searchTerm)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        return await WithTagIncludes(_dbSet)
            .Where(m =>
                m.Name.ToLower().Contains(lowerSearchTerm) ||
                m.PublicBio.ToLower().Contains(lowerSearchTerm) ||
                m.MentorTags.Any(mt => mt.Tag.Name.ToLower().Contains(lowerSearchTerm)))
            .ToListAsync();
    }

    public async Task<IEnumerable<Mentor>> GetPopularAsync(int limit, int offset)
    {
        return await WithTagIncludes(_dbSet)
            .OrderByDescending(m => m.FollowerCount)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Mentor>> GetByCreatorAsync(Guid userId)
    {
        return await WithTagIncludes(_dbSet)
            .Where(m => m.CreatedBy == userId)
            .ToListAsync();
    }

    public async Task<bool> IsOwnerAsync(Guid mentorId, Guid userId)
    {
        return await _dbSet
            .AnyAsync(m => m.Id == mentorId && m.CreatedBy == userId);
    }

    public async Task<IEnumerable<Mentor>> GetFollowedByUserAsync(Guid userId, int limit, int offset)
    {
        return await WithTagIncludes(_dbSet)
            .Where(m => _context.UserFollowsMentor.Any(ufm => ufm.UserId == userId && ufm.MentorId == m.Id))
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Mentor>> GetByCreatorWithPaginationAsync(Guid userId, int limit, int offset)
    {
        return await WithTagIncludes(_dbSet)
            .Where(m => m.CreatedBy == userId && m.DeletedAt == null)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }
}
