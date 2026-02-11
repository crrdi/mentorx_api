using AutoMapper;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;
using MentorX.Application.Interfaces;
using MentorX.Domain.Interfaces;
using MentorX.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MentorX.Application.Services;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IGeminiService _geminiService;
    private readonly Microsoft.Extensions.Logging.ILogger<CommentService> _logger;

    public CommentService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IGeminiService geminiService,
        Microsoft.Extensions.Logging.ILogger<CommentService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<PagedResponse<CommentResponse>> GetCommentsByInsightIdAsync(Guid insightId, int limit, int offset)
    {
        // Get total count
        var total = await _unitOfWork.Comments.GetTopLevelCountByInsightIdAsync(insightId);

        var comments = await _unitOfWork.Comments.GetTopLevelByInsightIdAsync(insightId, limit, offset);
        var responses = new List<CommentResponse>();

        foreach (var comment in comments)
        {
            var response = _mapper.Map<CommentResponse>(comment);

            // Get author info
            var actor = await _unitOfWork.Actors.GetByIdAsync(comment.AuthorActorId);
            if (actor != null)
            {
                if (actor.Type == ActorType.User && actor.UserId.HasValue)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(actor.UserId.Value);
                    response.Author = new AuthorResponse
                    {
                        Id = user!.Id,
                        Name = user.Name,
                        Type = "user"
                    };
                }
                else if (actor.Type == ActorType.Mentor && actor.MentorId.HasValue)
                {
                    var mentor = await _unitOfWork.Mentors.GetByIdAsync(actor.MentorId.Value);
                    response.Author = new AuthorResponse
                    {
                        Id = mentor!.Id,
                        Name = mentor.Name,
                        Type = "mentor"
                    };
                }
            }

            responses.Add(response);
        }

        return new PagedResponse<CommentResponse>
        {
            Items = responses,
            Total = total,
            HasMore = (offset + responses.Count) < total,
            Limit = limit,
            Offset = offset
        };
    }

    public async Task<CommentResponse> CreateCommentAsync(Guid insightId, Guid userId, CreateCommentRequest request)
    {
        var insight = await _unitOfWork.Insights.GetByIdAsync(insightId);
        if (insight == null)
        {
            throw new KeyNotFoundException("Insight not found");
        }

        // Actor kaydı olmalı - User oluşturulurken otomatik oluşturulmalı
        var userActor = await _unitOfWork.Actors.GetByUserIdAsync(userId);
        if (userActor == null)
        {
            _logger.LogError("User actor not found for userId {UserId}. Actor record should be created when user is registered.", userId);
            throw new InvalidOperationException($"User actor not found for user {userId}. Please ensure the user was properly registered and actor record was created.");
        }

        string content;
        Guid authorActorId;

        if (request.MentorId.HasValue)
        {
            // Check ownership
            if (!await _unitOfWork.Mentors.IsOwnerAsync(request.MentorId.Value, userId))
            {
                throw new UnauthorizedAccessException("You are not the owner of this mentor");
            }

            var mentorActor = await _unitOfWork.Actors.GetByMentorIdAsync(request.MentorId.Value);
            if (mentorActor == null)
            {
                throw new Exception("Mentor actor not found");
            }

            authorActorId = mentorActor.Id;

            // Generate content using Gemini AI - NO FALLBACK, throw exception on error
            var mentor = await _unitOfWork.Mentors.GetByIdAsync(request.MentorId.Value);
            if (mentor == null)
            {
                throw new Exception("Mentor not found");
            }

            var mentorHandle = $"mentor_{mentor.Id.ToString().Substring(0, 8)}";
            var tagNames = mentor.MentorTags.Select(mt => mt.Tag.Name).ToList();

            try
            {
                content = await _geminiService.GenerateCommentAsync(
                    mentor.Name,
                    mentorHandle,
                    mentor.ExpertisePrompt,
                    tagNames,
                    insight.Content,
                    CancellationToken.None);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("API key") || ex.Message.Contains("not configured"))
            {
                _logger.LogError(ex, "Gemini API key is not configured for comment generation.");
                throw new InvalidOperationException("Gemini API is not configured. Please contact administrator.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate comment using Gemini. Error Type: {ErrorType}, Message: {ErrorMessage}",
                    ex.GetType().Name,
                    ex.Message);

                // NO FALLBACK - Throw error instead of placeholder text
                throw new InvalidOperationException($"Failed to generate comment content: {ex.Message}", ex);
            }
        }
        else
        {
            if (string.IsNullOrEmpty(request.Content))
            {
                throw new ArgumentException("Content is required when MentorId is not provided");
            }

            authorActorId = userActor.Id;
            content = request.Content;
        }

        var comment = new Domain.Entities.Comment
        {
            Id = Guid.NewGuid(),
            InsightId = insightId,
            AuthorActorId = authorActorId,
            Content = content,
            ParentId = request.ParentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Comments.AddAsync(comment);
        await _unitOfWork.Insights.IncrementCommentCountAsync(insightId);
        await _unitOfWork.SaveChangesAsync();

        var response = _mapper.Map<CommentResponse>(comment);

        // Set author
        var actor = await _unitOfWork.Actors.GetByIdAsync(authorActorId);
        if (actor != null && actor.Type == ActorType.Mentor && actor.MentorId.HasValue)
        {
            var mentor = await _unitOfWork.Mentors.GetByIdAsync(actor.MentorId.Value);
            response.Author = new AuthorResponse
            {
                Id = mentor!.Id,
                Name = mentor.Name,
                Type = "mentor"
            };
        }
        else if (actor != null && actor.Type == ActorType.User && actor.UserId.HasValue)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(actor.UserId.Value);
            response.Author = new AuthorResponse
            {
                Id = user!.Id,
                Name = user.Name,
                Type = "user"
            };
        }

        return response;
    }
}
