using Microsoft.EntityFrameworkCore;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.DbContext;

namespace MentorX.Infrastructure.Data.Repositories;

public class UserFollowsMentorRepository : IUserFollowsMentorRepository
{
    private readonly MentorXDbContext _context;

    public UserFollowsMentorRepository(MentorXDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsFollowingAsync(Guid userId, Guid mentorId)
    {
        return await _context.UserFollowsMentor
            .AnyAsync(ufm => ufm.UserId == userId && ufm.MentorId == mentorId);
    }

    public async Task FollowAsync(Guid userId, Guid mentorId)
    {
        var exists = await IsFollowingAsync(userId, mentorId);
        if (!exists)
        {
            _context.UserFollowsMentor.Add(new Domain.Entities.UserFollowsMentor
            {
                UserId = userId,
                MentorId = mentorId,
                CreatedAt = DateTime.UtcNow
            });
            
            // Increment follower count
            var mentor = await _context.Mentors.FindAsync(mentorId);
            if (mentor != null)
            {
                mentor.FollowerCount++;
            }
            
            await _context.SaveChangesAsync();
        }
    }

    public async Task UnfollowAsync(Guid userId, Guid mentorId)
    {
        var follow = await _context.UserFollowsMentor
            .FirstOrDefaultAsync(ufm => ufm.UserId == userId && ufm.MentorId == mentorId);
        
        if (follow != null)
        {
            _context.UserFollowsMentor.Remove(follow);
            
            // Decrement follower count
            var mentor = await _context.Mentors.FindAsync(mentorId);
            if (mentor != null)
            {
                mentor.FollowerCount = Math.Max(0, mentor.FollowerCount - 1);
            }
            
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Guid>> GetFollowedMentorIdsAsync(Guid userId)
    {
        return await _context.UserFollowsMentor
            .Where(ufm => ufm.UserId == userId)
            .Select(ufm => ufm.MentorId)
            .ToListAsync();
    }

    public async Task<int> GetFollowerCountAsync(Guid mentorId)
    {
        return await _context.UserFollowsMentor
            .CountAsync(ufm => ufm.MentorId == mentorId);
    }

    public async Task<IEnumerable<Domain.Entities.Mentor>> GetFollowedMentorsWithPaginationAsync(Guid userId, int limit, int offset)
    {
        return await _context.UserFollowsMentor
            .Where(ufm => ufm.UserId == userId)
            .Join(
                _context.Mentors.Where(m => m.DeletedAt == null),
                ufm => ufm.MentorId,
                m => m.Id,
                (ufm, m) => new { Mentor = m, FollowedAt = ufm.CreatedAt }
            )
            .OrderByDescending(x => x.FollowedAt)
            .Skip(offset)
            .Take(limit)
            .Select(x => x.Mentor)
            .ToListAsync();
    }
}
