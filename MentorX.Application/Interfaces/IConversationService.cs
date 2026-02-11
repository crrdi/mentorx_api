using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;

namespace MentorX.Application.Interfaces;

public interface IConversationService
{
    Task<PagedResponse<ConversationResponse>> GetConversationsAsync(Guid userId, int limit, int offset);
    Task<ConversationResponse> CreateConversationAsync(Guid userId, CreateConversationRequest request);
    Task<List<MessageResponse>> GetMessagesAsync(Guid conversationId, Guid userId);
    Task<MessageResponse> SendMessageAsync(Guid conversationId, Guid userId, CreateMessageRequest request);
}
