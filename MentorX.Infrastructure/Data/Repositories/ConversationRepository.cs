using Microsoft.EntityFrameworkCore;
using MentorX.Domain.Entities;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.DbContext;

namespace MentorX.Infrastructure.Data.Repositories;

public class ConversationRepository : Repository<Conversation>, IConversationRepository
{
    public ConversationRepository(MentorXDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Conversation>> GetByUserIdAsync(Guid userId, int limit, int offset)
    {
        return await _dbSet
            .Include(c => c.Mentor)
                .ThenInclude(m => m.Role)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .CountAsync(c => c.UserId == userId);
    }

    public async Task<Conversation?> GetByUserAndMentorAsync(Guid userId, Guid mentorId)
    {
        return await _dbSet
            .Include(c => c.Mentor)
                .ThenInclude(m => m.Role)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.MentorId == mentorId);
    }

    public async Task UpdateLastMessageAsync(Guid conversationId, string lastMessage)
    {
        var conversation = await _dbSet.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.LastMessage = lastMessage;
            conversation.LastMessageAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
