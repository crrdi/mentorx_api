using Microsoft.EntityFrameworkCore;
using MentorX.Domain.Entities;
using MentorX.Domain.Enums;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.DbContext;

namespace MentorX.Infrastructure.Data.Repositories;

public class ActorRepository : Repository<Actor>, IActorRepository
{
    public ActorRepository(MentorXDbContext context) : base(context)
    {
    }

    public async Task<Actor?> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Type == ActorType.User);
    }

    public async Task<Actor?> GetByMentorIdAsync(Guid mentorId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.MentorId == mentorId && a.Type == ActorType.Mentor);
    }

    public async Task<Actor?> GetByUserOrMentorIdAsync(Guid? userId, Guid? mentorId, ActorType type)
    {
        if (type == ActorType.User && userId.HasValue)
        {
            return await GetByUserIdAsync(userId.Value);
        }
        
        if (type == ActorType.Mentor && mentorId.HasValue)
        {
            return await GetByMentorIdAsync(mentorId.Value);
        }
        
        return null;
    }
}
