using MentorX.Domain.Entities;

namespace MentorX.Domain.Interfaces;

public interface IMessageRepository : IRepository<Message>
{
    Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId);
    Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId, int limit, int offset);
    Task<int> GetCountByConversationIdAsync(Guid conversationId);
}
