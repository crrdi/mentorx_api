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
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null || conversation.UserId != userId)
        {
            throw new UnauthorizedAccessException("Conversation not found or access denied");
        }

        var messages = await _unitOfWork.Messages.GetByConversationIdAsync(conversationId);
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

        return responses;
    }

    public async Task<MessageResponse> SendMessageAsync(Guid conversationId, Guid userId, CreateMessageRequest request)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null || conversation.UserId != userId)
        {
            throw new UnauthorizedAccessException("Conversation not found or access denied");
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

        var response = _mapper.Map<MessageResponse>(message);
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        response.Sender = new AuthorResponse
        {
            Id = user!.Id,
            Name = user.Name,
            Type = "user"
        };

        // Generate mentor reply asynchronously (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await GenerateMentorReplyAsync(conversationId, request.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate mentor reply for conversation {ConversationId}", conversationId);
            }
        });

        return response;
    }

    private async Task GenerateMentorReplyAsync(Guid conversationId, string userMessage)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null)
        {
            return;
        }

        var mentor = await _unitOfWork.Mentors.GetByIdAsync(conversation.MentorId);
        if (mentor == null)
        {
            return;
        }

        // Get conversation history (last 10 messages)
        var messages = await _unitOfWork.Messages.GetByConversationIdAsync(conversationId);
        var conversationHistory = messages
            .OrderBy(m => m.CreatedAt)
            .TakeLast(10)
            .Select(m => $"{m.SenderActorId}: {m.Content}")
            .ToList();

        // Generate mentor reply using Gemini
        string mentorReplyContent;
        try
        {
            var mentorActor = await _unitOfWork.Actors.GetByMentorIdAsync(mentor.Id);
            if (mentorActor == null)
            {
                return;
            }

            var mentorHandle = $"mentor_{mentor.Id.ToString().Substring(0, 8)}";
            var tagNames = mentor.MentorTags.Select(mt => mt.Tag.Name).ToList();

            mentorReplyContent = await _geminiService.GenerateDirectMessageAsync(
                mentor.Name,
                mentorHandle,
                mentor.ExpertisePrompt,
                tagNames,
                userMessage,
                conversationHistory,
                CancellationToken.None);

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

            _logger.LogInformation("Generated mentor reply for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating mentor reply for conversation {ConversationId}", conversationId);
        }
    }
}
