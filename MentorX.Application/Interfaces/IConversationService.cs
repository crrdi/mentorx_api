using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;

namespace MentorX.Application.Interfaces;

public interface IConversationService
{
    Task<PagedResponse<ConversationResponse>> GetConversationsAsync(Guid userId, int limit, int offset);
    Task<ConversationResponse> CreateConversationAsync(Guid userId, CreateConversationRequest request);
    Task<List<MessageResponse>> GetMessagesAsync(Guid conversationId, Guid userId);
    Task<PagedResponse<MessageResponse>> GetMessagesAsync(Guid conversationId, Guid userId, int limit, int offset);
    Task<SendMessageResponse> SendMessageAsync(Guid conversationId, Guid userId, CreateMessageRequest request);
}
