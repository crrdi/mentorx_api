using Microsoft.EntityFrameworkCore;
using MentorX.Domain.Entities;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.DbContext;

namespace MentorX.Infrastructure.Data.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(MentorXDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(u => u.Email == email);
    }

    public async Task<User?> GetByRevenueCatAppUserIdsAsync(string? appUserId, string? originalAppUserId, IReadOnlyList<string>? aliases)
    {
        var idsToTry = new List<string>();
        if (!string.IsNullOrEmpty(appUserId)) idsToTry.Add(appUserId);
        if (!string.IsNullOrEmpty(originalAppUserId) && !idsToTry.Contains(originalAppUserId)) idsToTry.Add(originalAppUserId);
        if (aliases != null)
        {
            foreach (var alias in aliases.Where(a => !string.IsNullOrEmpty(a) && !idsToTry.Contains(a)))
            {
                idsToTry.Add(alias);
            }
        }

        // First, try to find by Guid (User.Id)
        foreach (var id in idsToTry)
        {
            if (Guid.TryParse(id, out var userId))
            {
                var user = await _dbSet.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null) return user;
            }
        }

        // If not found by Guid, try to find by RevenueCatCustomerId (for anonymous IDs like $RCAnonymousID:...)
        foreach (var id in idsToTry)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var user = await _dbSet.FirstOrDefaultAsync(u => u.RevenueCatCustomerId == id);
                if (user != null) return user;
            }
        }

        return null;
    }
}
