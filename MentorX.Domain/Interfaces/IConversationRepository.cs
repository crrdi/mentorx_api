using MentorX.Domain.Entities;

namespace MentorX.Domain.Interfaces;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<IEnumerable<Conversation>> GetByUserIdAsync(Guid userId, int limit, int offset);
    Task<int> GetCountByUserIdAsync(Guid userId);
    Task<Conversation?> GetByUserAndMentorAsync(Guid userId, Guid mentorId);
    Task UpdateLastMessageAsync(Guid conversationId, string lastMessage);
}
