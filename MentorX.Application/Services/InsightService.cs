using AutoMapper;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;
using MentorX.Application.Interfaces;
using MentorX.Domain.Interfaces;
using MentorX.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MentorX.Application.Services;

public class InsightService : IInsightService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<InsightService> _logger;
    private readonly IGeminiService _geminiService;

    public InsightService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<InsightService> logger, IGeminiService geminiService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _geminiService = geminiService;
    }

    public async Task<PagedResponse<InsightResponse>> GetInsightsAsync(string? tag, Guid? mentorId, string? mentorIds, string? sort, int limit, int offset, Guid? userId)
    {
        _logger.LogInformation("[GetInsightsAsync] Called with tag: {Tag}, mentorId: {MentorId}, mentorIds: {MentorIds}, sort: {Sort}, limit: {Limit}, offset: {Offset}",
            tag, mentorId, mentorIds, sort, limit, offset);

        IEnumerable<Domain.Entities.Insight> insights;

        if (!string.IsNullOrEmpty(tag))
        {
            _logger.LogInformation("[GetInsightsAsync] Fetching insights by tag: {Tag}, sort: {Sort}", tag, sort);
            
            // Get total count for pagination
            var tagTotal = await _unitOfWork.Insights.GetCountByTagAsync(tag);
            
            // Get insights with sort
            insights = await _unitOfWork.Insights.GetByTagAsync(tag, sort ?? "latest", limit, offset);
            var insightsList = insights.ToList();
            _logger.LogInformation("[GetInsightsAsync] Found {Count} insights for tag: {Tag} (total: {Total})", insightsList.Count, tag, tagTotal);
            
            if (insightsList.Any())
            {
                var firstInsight = insightsList.First();
                _logger.LogInformation("[GetInsightsAsync] First insight ID: {InsightId}, InsightTags count: {TagCount}",
                    firstInsight.Id, firstInsight.InsightTags?.Count ?? 0);
                if (firstInsight.InsightTags != null && firstInsight.InsightTags.Any())
                {
                    var tagNames = firstInsight.InsightTags.Select(it => it.Tag?.Name ?? "null").ToList();
                    _logger.LogInformation("[GetInsightsAsync] First insight tags: {Tags}", string.Join(", ", tagNames));
                }
            }
            
            var tagInsightResponses = insights.Select(i => _mapper.Map<InsightResponse>(i)).ToList();

            // Set IsLiked if userId provided
            if (userId.HasValue)
            {
                foreach (var insight in tagInsightResponses)
                {
                    insight.IsLiked = await _unitOfWork.UserLikes.IsLikedAsync(userId.Value, insight.Id);
                }
            }

            return new PagedResponse<InsightResponse>
            {
                Items = tagInsightResponses,
                Total = tagTotal,
                HasMore = (offset + tagInsightResponses.Count) < tagTotal,
                Limit = limit,
                Offset = offset
            };
        }
        else if (mentorId.HasValue)
        {
            insights = await _unitOfWork.Insights.GetByMentorIdAsync(mentorId.Value, limit, offset);
        }
        else if (!string.IsNullOrEmpty(mentorIds))
        {
            var ids = mentorIds.Split(',').Select(Guid.Parse).ToList();
            insights = await _unitOfWork.Insights.GetByMentorIdsAsync(ids, limit, offset);
        }
        else if (sort == "popular")
        {
            insights = await _unitOfWork.Insights.GetPopularAsync(limit, offset);
        }
        else
        {
            insights = await _unitOfWork.Insights.GetLatestAsync(limit, offset);
        }

        var insightResponses = insights.Select(i => _mapper.Map<InsightResponse>(i)).ToList();

        // Set IsLiked if userId provided
        if (userId.HasValue)
        {
            foreach (var insight in insightResponses)
            {
                insight.IsLiked = await _unitOfWork.UserLikes.IsLikedAsync(userId.Value, insight.Id);
            }
        }

        // Calculate total for non-tag queries
        var total = insightResponses.Count;

        return new PagedResponse<InsightResponse>
        {
            Items = insightResponses,
            Total = total,
            HasMore = insightResponses.Count == limit,
            Limit = limit,
            Offset = offset
        };
    }

    public async Task<InsightResponse?> GetInsightByIdAsync(Guid id, Guid? userId)
    {
        var insight = await _unitOfWork.Insights.GetByIdAsync(id);
        if (insight == null)
        {
            return null;
        }

        var response = _mapper.Map<InsightResponse>(insight);
        
        if (userId.HasValue)
        {
            response.IsLiked = await _unitOfWork.UserLikes.IsLikedAsync(userId.Value, id);
        }

        return response;
    }

    public async Task<InsightResponse> CreateInsightAsync(Guid userId, CreateInsightRequest request)
    {
        // Check credits (but don't deduct yet)
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.Credits < 1)
        {
            throw new InvalidOperationException("Insufficient credits");
        }

        // Check ownership
        if (!await _unitOfWork.Mentors.IsOwnerAsync(request.MentorId, userId))
        {
            throw new UnauthorizedAccessException("You are not the owner of this mentor");
        }

        // Get mentor for content generation
        var mentor = await _unitOfWork.Mentors.GetByIdAsync(request.MentorId);
        if (mentor == null)
        {
            throw new KeyNotFoundException("Mentor not found");
        }

        var tagNames = request.Tags ?? mentor.MentorTags.Select(mt => mt.Tag.Name).ToList();
        var tagEntities = await _unitOfWork.Tags.GetOrCreateManyAsync(tagNames);

        // Generate content using Gemini AI FIRST - NO FALLBACK, throw exception on error
        // Only deduct credit after successful generation
        var mentorHandle = $"mentor_{mentor.Id.ToString().Substring(0, 8)}";
        _logger.LogInformation("Generating post content using Gemini for mentor {MentorId} ({MentorName})", mentor.Id, mentor.Name);
        
        string content;
        try
        {
            content = await _geminiService.GeneratePostAsync(
                mentor.Name,
                mentorHandle,
                mentor.ExpertisePrompt,
                tagNames,
                CancellationToken.None);
            
            _logger.LogInformation("Successfully generated post content: {Content}", content);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key") || ex.Message.Contains("not configured"))
        {
            _logger.LogError(ex, "Gemini API key is not configured. Please configure it using User Secrets or Environment Variables.");
            throw new InvalidOperationException("Gemini API is not configured. Please contact administrator.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate content using Gemini. Error Type: {ErrorType}, Message: {ErrorMessage}, InnerException: {InnerException}", 
                ex.GetType().Name, 
                ex.Message, 
                ex.InnerException?.Message ?? "None");
            
            // NO FALLBACK - Throw error before deducting credit
            throw new InvalidOperationException("Failed to generate post content. The AI service encountered an error. Please try again.", ex);
        }

        // Only deduct credit after successful content generation
        user.Credits--;
        await _unitOfWork.Users.UpdateAsync(user);
        _logger.LogInformation("Credit deducted for user {UserId}. Remaining credits: {Credits}", userId, user.Credits);

        InsightType insightType = InsightType.Insight;
        Guid? masterclassPostId = null;

        var insight = new Domain.Entities.Insight
        {
            Id = Guid.NewGuid(),
            MentorId = request.MentorId,
            Content = content,
            Quote = request.Quote,
            HasMedia = request.HasMedia,
            MediaUrl = request.MediaUrl,
            Type = insightType,
            MasterclassPostId = masterclassPostId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        insight.InsightTags = tagEntities.Select(t => new Domain.Entities.InsightTag { InsightId = insight.Id, TagId = t.Id }).ToList();
        await _unitOfWork.Insights.AddAsync(insight);
        // Note: CommentCount starts at 0. It will be incremented automatically by trigger when comments are added.
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<InsightResponse>(insight);
    }

    public async Task<List<InsightResponse>> CreateThreadAsync(Guid userId, CreateInsightRequest request)
    {
        // Check credits (but don't deduct yet)
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.Credits < 1)
        {
            throw new InvalidOperationException("Insufficient credits");
        }

        // Check ownership
        if (!await _unitOfWork.Mentors.IsOwnerAsync(request.MentorId, userId))
        {
            throw new UnauthorizedAccessException("You are not the owner of this mentor");
        }

        // Get mentor for content generation
        var mentor = await _unitOfWork.Mentors.GetByIdAsync(request.MentorId);
        if (mentor == null)
        {
            throw new KeyNotFoundException("Mentor not found");
        }

        var tagNames = request.Tags ?? mentor.MentorTags.Select(mt => mt.Tag.Name).ToList();
        var tagEntities = await _unitOfWork.Tags.GetOrCreateManyAsync(tagNames);

        // Generate thread using Gemini AI FIRST - NO FALLBACK, throw exception on error
        // Only deduct credit after successful generation
        var mentorHandle = $"mentor_{mentor.Id.ToString().Substring(0, 8)}";
        _logger.LogInformation("Generating thread content using Gemini for mentor {MentorId} ({MentorName})", mentor.Id, mentor.Name);
        
        List<string> threadContents;
        try
        {
            threadContents = await _geminiService.GenerateThreadAsync(
                mentor.Name,
                mentorHandle,
                mentor.ExpertisePrompt,
                tagNames,
                CancellationToken.None);
            
            _logger.LogInformation("Successfully generated thread with {Count} posts", threadContents.Count);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key") || ex.Message.Contains("not configured"))
        {
            _logger.LogError(ex, "Gemini API key is not configured. Please configure it using User Secrets or Environment Variables.");
            throw new InvalidOperationException("Gemini API is not configured. Please contact administrator.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thread using Gemini. Error Type: {ErrorType}, Message: {ErrorMessage}", 
                ex.GetType().Name, 
                ex.Message);
            
            // NO FALLBACK - Throw error before deducting credit
            throw new InvalidOperationException("Failed to generate thread content. The AI service encountered an error. Please try again.", ex);
        }

        // Only deduct credit after successful content generation
        user.Credits--;
        await _unitOfWork.Users.UpdateAsync(user);
        _logger.LogInformation("Credit deducted for user {UserId} for thread creation. Remaining credits: {Credits}", userId, user.Credits);

        // Create master insight (first post)
        var masterInsight = new Domain.Entities.Insight
        {
            Id = Guid.NewGuid(),
            MentorId = request.MentorId,
            Content = threadContents[0],
            Quote = request.Quote,
            HasMedia = request.HasMedia,
            MediaUrl = request.MediaUrl,
            Type = InsightType.Masterclass,
            MasterclassPostId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        masterInsight.InsightTags = tagEntities.Select(t => new Domain.Entities.InsightTag { InsightId = masterInsight.Id, TagId = t.Id }).ToList();
        await _unitOfWork.Insights.AddAsync(masterInsight);

        // Create thread posts (remaining posts)
        var threadInsights = new List<Domain.Entities.Insight>();
        for (int i = 1; i < threadContents.Count; i++)
        {
            var threadPost = new Domain.Entities.Insight
            {
                Id = Guid.NewGuid(),
                MentorId = request.MentorId,
                Content = threadContents[i],
                Quote = null,
                HasMedia = false,
                MediaUrl = null,
                Type = InsightType.Masterclass,
                MasterclassPostId = masterInsight.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            threadPost.InsightTags = tagEntities.Select(t => new Domain.Entities.InsightTag { InsightId = threadPost.Id, TagId = t.Id }).ToList();
            threadInsights.Add(threadPost);
            await _unitOfWork.Insights.AddAsync(threadPost);
        }

        // Note: CommentCount starts at 0. It will be incremented automatically by trigger when comments are added.
        await _unitOfWork.SaveChangesAsync();

        var responses = new List<InsightResponse> { _mapper.Map<InsightResponse>(masterInsight) };
        responses.AddRange(threadInsights.Select(t => _mapper.Map<InsightResponse>(t)));
        
        return responses;
    }

    public async Task<PagedResponse<InsightResponse>> GetFeedAsync(Guid userId, string? tag, int limit, int offset)
    {
        var followedMentorIds = await _unitOfWork.UserFollowsMentor.GetFollowedMentorIdsAsync(userId);
        var mentorIdsList = followedMentorIds.ToList();

        // Takip edilen mentor yoksa anasayfada tüm postların en son eklenenleri (discover) dönsün
        if (!mentorIdsList.Any())
        {
            IEnumerable<Domain.Entities.Insight> latest;
            if (tag != null)
            {
                latest = await _unitOfWork.Insights.GetByTagAsync(tag, "latest", limit, offset);
            }
            else
            {
                latest = await _unitOfWork.Insights.GetLatestAsync(limit, offset);
            }
            var list = latest.ToList();
            var responses = new List<InsightResponse>();
            foreach (var i in list)
            {
                var response = _mapper.Map<InsightResponse>(i);
                response.IsLiked = await _unitOfWork.UserLikes.IsLikedAsync(userId, i.Id);
                responses.Add(response);
            }
            return new PagedResponse<InsightResponse>
            {
                Items = responses,
                Total = responses.Count,
                HasMore = responses.Count == limit,
                Limit = limit,
                Offset = offset
            };
        }

        // Get total count before pagination
        var total = await _unitOfWork.Insights.GetCountByMentorIdsAsync(mentorIdsList, tag);

        var insights = await _unitOfWork.Insights.GetByMentorIdsAsync(mentorIdsList, limit, offset, tag);

        var insightResponses = new List<InsightResponse>();
        foreach (var i in insights)
        {
            var response = _mapper.Map<InsightResponse>(i);
            response.IsLiked = await _unitOfWork.UserLikes.IsLikedAsync(userId, i.Id);
            insightResponses.Add(response);
        }

        return new PagedResponse<InsightResponse>
        {
            Items = insightResponses,
            Total = total,
            HasMore = (offset + insightResponses.Count) < total,
            Limit = limit,
            Offset = offset
        };
    }

    public async Task<LikeResponse> LikeInsightAsync(Guid userId, Guid insightId)
    {
        var insight = await _unitOfWork.Insights.GetByIdAsync(insightId);
        if (insight == null)
        {
            throw new KeyNotFoundException("Insight not found");
        }

        await _unitOfWork.UserLikes.LikeAsync(userId, insightId);
        await _unitOfWork.SaveChangesAsync();

        return new LikeResponse
        {
            Success = true,
            Liked = true
        };
    }

    public async Task<LikeResponse> UnlikeInsightAsync(Guid userId, Guid insightId)
    {
        var insight = await _unitOfWork.Insights.GetByIdAsync(insightId);
        if (insight == null)
        {
            throw new KeyNotFoundException("Insight not found");
        }

        await _unitOfWork.UserLikes.UnlikeAsync(userId, insightId);
        await _unitOfWork.SaveChangesAsync();

        return new LikeResponse
        {
            Success = true,
            Liked = false
        };
    }
}
