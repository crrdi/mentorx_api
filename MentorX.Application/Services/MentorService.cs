using AutoMapper;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;
using MentorX.Application.Interfaces;
using MentorX.Domain.Interfaces;
using MentorX.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MentorX.Application.Services;

public class MentorService : IMentorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<MentorService> _logger;
    private readonly IGeminiService _geminiService;
    private readonly IStorageService _storageService;

    public MentorService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<MentorService> logger, IGeminiService geminiService, IStorageService storageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _geminiService = geminiService;
        _storageService = storageService;
    }

    public async Task<PagedResponse<MentorResponse>> GetMentorsAsync(string? tag, bool popular, bool followed, string? search, int limit, int offset, Guid? userId)
    {
        _logger.LogInformation("GetMentorsAsync called with tag={Tag}, popular={Popular}, followed={Followed}, search={Search}, limit={Limit}, offset={Offset}, userId={UserId}",
            tag, popular, followed, search, limit, offset, userId);

        IEnumerable<Domain.Entities.Mentor> mentors;

        if (followed && userId.HasValue)
        {
            _logger.LogInformation("Fetching followed mentors for userId={UserId}", userId.Value);
            mentors = await _unitOfWork.Mentors.GetFollowedByUserAsync(userId.Value, limit, offset);
            _logger.LogInformation("Found {Count} followed mentors", mentors.Count());
        }
        else if (!string.IsNullOrEmpty(tag))
        {
            _logger.LogInformation("Fetching mentors by tag={Tag}", tag);
            mentors = await _unitOfWork.Mentors.GetByTagAsync(tag);
            mentors = mentors.Skip(offset).Take(limit);
            _logger.LogInformation("Found {Count} mentors with tag", mentors.Count());
        }
        else if (!string.IsNullOrEmpty(search))
        {
            _logger.LogInformation("Searching mentors with term={Search}", search);
            mentors = await _unitOfWork.Mentors.SearchAsync(search);
            mentors = mentors.Skip(offset).Take(limit);
            _logger.LogInformation("Found {Count} mentors matching search", mentors.Count());
        }
        else if (popular)
        {
            _logger.LogInformation("Fetching popular mentors");
            mentors = await _unitOfWork.Mentors.GetPopularAsync(limit, offset);
            _logger.LogInformation("Found {Count} popular mentors", mentors.Count());
        }
        else
        {
            _logger.LogInformation("Fetching all mentors");
            mentors = await _unitOfWork.Mentors.GetAllAsync();
            mentors = mentors.Skip(offset).Take(limit);
            _logger.LogInformation("Found {Count} mentors total", mentors.Count());
        }

        var mentorResponses = mentors.Select(m => _mapper.Map<MentorResponse>(m)).ToList();
        _logger.LogInformation("Mapped {Count} mentors to responses", mentorResponses.Count);

        // Set IsFollowing if userId provided
        if (userId.HasValue)
        {
            _logger.LogInformation("Setting IsFollowing flag for userId={UserId}", userId.Value);
            foreach (var mentor in mentorResponses)
            {
                mentor.IsFollowing = await _unitOfWork.UserFollowsMentor.IsFollowingAsync(userId.Value, mentor.Id);
            }
        }

        var response = new PagedResponse<MentorResponse>
        {
            Items = mentorResponses,
            Total = mentorResponses.Count,
            HasMore = mentorResponses.Count == limit,
            Limit = limit,
            Offset = offset
        };

        _logger.LogInformation("Returning {Count} mentors in response", response.Items.Count);
        return response;
    }

    public async Task<MentorResponse?> GetMentorByIdAsync(Guid id, Guid? userId)
    {
        var mentor = await _unitOfWork.Mentors.GetByIdAsync(id);
        if (mentor == null)
        {
            return null;
        }

        var response = _mapper.Map<MentorResponse>(mentor);
        
        if (userId.HasValue)
        {
            response.IsFollowing = await _unitOfWork.UserFollowsMentor.IsFollowingAsync(userId.Value, id);
            
            // Include ExpertisePrompt only if user is the owner
            if (await _unitOfWork.Mentors.IsOwnerAsync(id, userId.Value))
            {
                response.ExpertisePrompt = mentor.ExpertisePrompt;
            }
        }

        return response;
    }

    public async Task<MentorResponse> CreateMentorAsync(Guid userId, CreateMentorRequest request)
    {
        // Get default role - for now create if not exists
        // In production, this should be seeded in database
        var defaultRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Default MENTOR role ID

        var tagNames = request.ExpertiseTags.Select(t => t.StartsWith("#") ? t.Substring(1) : t).ToList();
        var tagEntities = await _unitOfWork.Tags.GetOrCreateManyAsync(tagNames);

        var mentor = new Domain.Entities.Mentor
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            PublicBio = request.PublicBio,
            ExpertisePrompt = request.ExpertisePrompt,
            Level = 1,
            RoleId = defaultRoleId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        mentor.MentorTags = tagEntities.Select(t => new Domain.Entities.MentorTag { MentorId = mentor.Id, TagId = t.Id }).ToList();
        await _unitOfWork.Mentors.AddAsync(mentor);

        // Actor kaydı trigger ile otomatik oluşturulacak (02-triggers.sql)

        await _unitOfWork.SaveChangesAsync();

        // Generate and upload avatar asynchronously - do not block mentor creation on failure
        try
        {
            var avatarBytes = await _geminiService.GenerateAvatarImageAsync(mentor.Name, mentor.PublicBio, tagNames);
            if (avatarBytes != null && avatarBytes.Length > 0)
            {
                var avatarUrl = await _storageService.UploadMentorAvatarAsync(mentor.Id, avatarBytes);
                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    mentor.Avatar = avatarUrl;
                    await _unitOfWork.Mentors.UpdateAsync(mentor);
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Avatar generated and saved for mentor {MentorId}", mentor.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Avatar generation failed for mentor {MentorId} - mentor created without avatar", mentor.Id);
        }

        var response = _mapper.Map<MentorResponse>(mentor);
        // Include ExpertisePrompt since user is the creator/owner
        response.ExpertisePrompt = mentor.ExpertisePrompt;
        return response;
    }

    public async Task<MentorResponse> UpdateMentorAsync(Guid mentorId, Guid userId, UpdateMentorRequest request)
    {
        var mentor = await _unitOfWork.Mentors.GetByIdAsync(mentorId);
        if (mentor == null)
        {
            throw new KeyNotFoundException("Mentor not found");
        }

        if (!await _unitOfWork.Mentors.IsOwnerAsync(mentorId, userId))
        {
            throw new UnauthorizedAccessException("You are not the owner of this mentor");
        }

        mentor.Name = request.Name;
        mentor.PublicBio = request.PublicBio;
        mentor.ExpertisePrompt = request.ExpertisePrompt;

        var tagNames = request.ExpertiseTags.Select(t => t.StartsWith("#") ? t.Substring(1) : t).ToList();
        var tagEntities = await _unitOfWork.Tags.GetOrCreateManyAsync(tagNames);
        mentor.MentorTags.Clear();
        foreach (var t in tagEntities)
            mentor.MentorTags.Add(new Domain.Entities.MentorTag { MentorId = mentor.Id, TagId = t.Id });

        await _unitOfWork.Mentors.UpdateAsync(mentor);
        await _unitOfWork.SaveChangesAsync();

        var response = _mapper.Map<MentorResponse>(mentor);
        // Include ExpertisePrompt since user is the owner (already verified above)
        response.ExpertisePrompt = mentor.ExpertisePrompt;
        return response;
    }

    public async Task<SuccessResponse> FollowMentorAsync(Guid mentorId, Guid userId)
    {
        await _unitOfWork.UserFollowsMentor.FollowAsync(userId, mentorId);
        return new SuccessResponse { Success = true };
    }

    public async Task<SuccessResponse> UnfollowMentorAsync(Guid mentorId, Guid userId)
    {
        await _unitOfWork.UserFollowsMentor.UnfollowAsync(userId, mentorId);
        return new SuccessResponse { Success = true };
    }

    public async Task<MentorRepliesResponse> GetMentorRepliesAsync(Guid mentorId)
    {
        var actor = await _unitOfWork.Actors.GetByMentorIdAsync(mentorId);
        if (actor == null)
        {
            return new MentorRepliesResponse();
        }

        var comments = await _unitOfWork.Comments.GetByAuthorActorIdAsync(actor.Id);
        
        var replies = new List<MentorReplyItem>();
        foreach (var comment in comments)
        {
            var insight = await _unitOfWork.Insights.GetByIdAsync(comment.InsightId);
            if (insight != null)
            {
                replies.Add(new MentorReplyItem
                {
                    Comment = _mapper.Map<CommentResponse>(comment),
                    ParentPost = _mapper.Map<InsightResponse>(insight)
                });
            }
        }

        return new MentorRepliesResponse { Replies = replies };
    }
}
