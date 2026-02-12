using Microsoft.EntityFrameworkCore;
using MentorX.Domain.Entities;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.DbContext;

namespace MentorX.Infrastructure.Data.Repositories;

public class MessageRepository : Repository<Message>, IMessageRepository
{
    public MessageRepository(MentorXDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId)
    {
        return await _dbSet
            .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId, int limit, int offset)
    {
        return await _dbSet
            .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetCountByConversationIdAsync(Guid conversationId)
    {
        return await _dbSet
            .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
            .CountAsync();
    }
}
