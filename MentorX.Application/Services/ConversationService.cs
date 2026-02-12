using AutoMapper;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;
using MentorX.Application.Interfaces;
using MentorX.Domain.Interfaces;
using MentorX.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MentorX.Application.Services;

public class ConversationService : IConversationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IGeminiService _geminiService;
    private readonly Microsoft.Extensions.Logging.ILogger<ConversationService> _logger;

    public ConversationService(
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        IGeminiService geminiService,
        Microsoft.Extensions.Logging.ILogger<ConversationService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<PagedResponse<ConversationResponse>> GetConversationsAsync(Guid userId, int limit, int offset)
    {
        // Get total count
        var total = await _unitOfWork.Conversations.GetCountByUserIdAsync(userId);
        
        var conversations = await _unitOfWork.Conversations.GetByUserIdAsync(userId, limit, offset);
        var responses = conversations.Select(c => _mapper.Map<ConversationResponse>(c)).ToList();

        return new PagedResponse<ConversationResponse>
        {
            Items = responses,
            Total = total,
            HasMore = (offset + responses.Count) < total,
            Limit = limit,
            Offset = offset
        };
    }

    public async Task<ConversationResponse> CreateConversationAsync(Guid userId, CreateConversationRequest request)
    {
        // Check if conversation already exists
        var existing = await _unitOfWork.Conversations.GetByUserAndMentorAsync(userId, request.MentorId);
        if (existing != null)
        {
            return _mapper.Map<ConversationResponse>(existing);
        }

        var mentor = await _unitOfWork.Mentors.GetByIdAsync(request.MentorId);
        if (mentor == null)
        {
            throw new KeyNotFoundException("Mentor not found");
        }

        var conversation = new Domain.Entities.Conversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MentorId = request.MentorId,
            LastMessage = string.Empty,
            LastMessageAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Conversations.AddAsync(conversation);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ConversationResponse>(conversation);
    }

    public async Task<List<MessageResponse>> GetMessagesAsync(Guid conversationId, Guid userId)
    {
        // Backward compatibility - get all messages
        var pagedResult = await GetMessagesAsync(conversationId, userId, int.MaxValue, 0);
        return pagedResult.Items.ToList();
    }

    public async Task<PagedResponse<MessageResponse>> GetMessagesAsync(Guid conversationId, Guid userId, int limit, int offset)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null || conversation.UserId != userId)
        {
            throw new UnauthorizedAccessException("Conversation not found or access denied");
        }

        // Get total count
        var total = await _unitOfWork.Messages.GetCountByConversationIdAsync(conversationId);

        // Get paginated messages
        var messages = await _unitOfWork.Messages.GetByConversationIdAsync(conversationId, limit, offset);
        var responses = new List<MessageResponse>();

        foreach (var message in messages)
        {
            var response = _mapper.Map<MessageResponse>(message);
            
            var actor = await _unitOfWork.Actors.GetByIdAsync(message.SenderActorId);
            if (actor != null)
            {
                if (actor.Type == ActorType.User && actor.UserId.HasValue)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(actor.UserId.Value);
                    response.Sender = new AuthorResponse
                    {
                        Id = user!.Id,
                        Name = user.Name,
                        Type = "user"
                    };
                }
                else if (actor.Type == ActorType.Mentor && actor.MentorId.HasValue)
                {
                    var mentor = await _unitOfWork.Mentors.GetByIdAsync(actor.MentorId.Value);
                    response.Sender = new AuthorResponse
                    {
                        Id = mentor!.Id,
                        Name = mentor.Name,
                        Type = "mentor"
                    };
                }
            }

            responses.Add(response);
        }

        return new PagedResponse<MessageResponse>
        {
            Items = responses,
            Total = total,
            HasMore = (offset + responses.Count) < total,
            Limit = limit,
            Offset = offset
        };
    }

    public async Task<SendMessageResponse> SendMessageAsync(Guid conversationId, Guid userId, CreateMessageRequest request)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null || conversation.UserId != userId)
        {
            throw new UnauthorizedAccessException("Conversation not found or access denied");
        }

        // Check user credits before allowing message to be sent
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        if (user.Credits < 1)
        {
            throw new InvalidOperationException("Insufficient credits");
        }

        // Actor kaydı olmalı - User oluşturulurken otomatik oluşturulmalı
        var userActor = await _unitOfWork.Actors.GetByUserIdAsync(userId);
        if (userActor == null)
        {
            _logger.LogError("User actor not found for userId {UserId}. Actor record should be created when user is registered.", userId);
            throw new InvalidOperationException($"User actor not found for user {userId}. Please ensure the user was properly registered and actor record was created.");
        }

        var message = new Domain.Entities.Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderActorId = userActor.Id,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Messages.AddAsync(message);
        await _unitOfWork.Conversations.UpdateLastMessageAsync(conversationId, request.Content);
        await _unitOfWork.SaveChangesAsync();

        // Deduct credit after message is successfully saved
        user.Credits--;
        await _unitOfWork.Users.UpdateAsync(user);

        // Create credit transaction record for audit trail
        var transaction = new Domain.Entities.CreditTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = CreditTransactionType.Deduction,
            Amount = -1,
            BalanceAfter = user.Credits,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _unitOfWork.CreditTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Credit deducted for user {UserId} for sending message. Remaining credits: {Credits}", userId, user.Credits);

        var userMessageResponse = _mapper.Map<MessageResponse>(message);
        userMessageResponse.Sender = new AuthorResponse
        {
            Id = user.Id,
            Name = user.Name,
            Type = "user"
        };

        // Generate mentor reply synchronously and return in response
        MessageResponse? mentorReplyResponse = null;
        try
        {
            mentorReplyResponse = await GenerateMentorReplyAsync(conversationId, request.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate mentor reply for conversation {ConversationId}. User message was saved successfully.", conversationId);
            // Continue without mentor reply - user message is already saved
        }

        return new SendMessageResponse
        {
            UserMessage = userMessageResponse,
            MentorReply = mentorReplyResponse
        };
    }

    private async Task<MessageResponse?> GenerateMentorReplyAsync(
        Guid conversationId, 
        string userMessage)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null)
        {
            _logger.LogWarning("Conversation {ConversationId} not found when generating mentor reply", conversationId);
            return null;
        }

        var mentor = await _unitOfWork.Mentors.GetByIdAsync(conversation.MentorId);
        if (mentor == null)
        {
            _logger.LogWarning("Mentor {MentorId} not found for conversation {ConversationId}", conversation.MentorId, conversationId);
            return null;
        }

        // Get conversation history (last 10 messages)
        var messages = await _unitOfWork.Messages.GetByConversationIdAsync(conversationId);
        var conversationHistory = messages
            .OrderBy(m => m.CreatedAt)
            .TakeLast(10)
            .Select(m => $"{m.SenderActorId}: {m.Content}")
            .ToList();

        // Generate mentor reply using Gemini
        var mentorActor = await _unitOfWork.Actors.GetByMentorIdAsync(mentor.Id);
        if (mentorActor == null)
        {
            _logger.LogWarning("Mentor actor not found for mentor {MentorId}", mentor.Id);
            return null;
        }

        var mentorHandle = $"mentor_{mentor.Id.ToString().Substring(0, 8)}";
        var tagNames = mentor.MentorTags.Select(mt => mt.Tag.Name).ToList();

        _logger.LogInformation("Generating mentor reply for conversation {ConversationId} using Gemini", conversationId);

        string mentorReplyContent;
        try
        {
            mentorReplyContent = await _geminiService.GenerateDirectMessageAsync(
                mentor.Name,
                mentorHandle,
                mentor.ExpertisePrompt,
                tagNames,
                userMessage,
                conversationHistory,
                CancellationToken.None);

            _logger.LogInformation("Successfully generated mentor reply content: {Content}", mentorReplyContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating mentor reply content for conversation {ConversationId}. Error: {ErrorMessage}", conversationId, ex.Message);
            throw; // Re-throw to be caught by outer try-catch
        }

        // Create mentor reply message
        var mentorReply = new Domain.Entities.Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderActorId = mentorActor.Id,
            Content = mentorReplyContent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Messages.AddAsync(mentorReply);
        await _unitOfWork.Conversations.UpdateLastMessageAsync(conversationId, mentorReplyContent);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Generated and saved mentor reply for conversation {ConversationId}", conversationId);

        // Map to response
        var mentorReplyResponse = _mapper.Map<MessageResponse>(mentorReply);
        mentorReplyResponse.Sender = new AuthorResponse
        {
            Id = mentor.Id,
            Name = mentor.Name,
            Type = "mentor"
        };

        return mentorReplyResponse;
    }
}
