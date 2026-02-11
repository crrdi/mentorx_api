using AutoMapper;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;
using MentorX.Application.Interfaces;
using MentorX.Domain.Interfaces;

namespace MentorX.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserResponse> GetCurrentUserAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        return _mapper.Map<UserResponse>(user);
    }

    public async Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            user.Name = request.Name;
        }

        if (!string.IsNullOrEmpty(request.Email))
        {
            // Check if email already exists
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != userId)
            {
                throw new InvalidOperationException("Email already in use");
            }
            user.Email = request.Email;
        }

        if (request.FocusAreas != null)
        {
            user.FocusAreas = request.FocusAreas;
        }

        if (request.Avatar != null)
        {
            user.Avatar = request.Avatar;
        }

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<UserResponse>(user);
    }

    public async Task<PagedResponse<MentorResponse>> GetCreatedMentorsAsync(Guid userId, int limit, int offset)
    {
        var mentors = await _unitOfWork.Mentors.GetByCreatorWithPaginationAsync(userId, limit, offset);
        var mentorList = mentors.ToList();
        
        var mentorResponses = mentorList.Select(m => _mapper.Map<MentorResponse>(m)).ToList();
        
        // Set IsFollowing flag (user is always following their own created mentors)
        foreach (var mentor in mentorResponses)
        {
            mentor.IsFollowing = true;
        }

        // Check if there are more records - fetch one extra to determine hasMore
        var totalCount = await _unitOfWork.Mentors.CountAsync(m => m.CreatedBy == userId && m.DeletedAt == null);
        var hasMore = (offset + limit) < totalCount;

        return new PagedResponse<MentorResponse>
        {
            Items = mentorResponses,
            Total = mentorResponses.Count,
            HasMore = hasMore,
            Limit = limit,
            Offset = offset
        };
    }

    public async Task<PagedResponse<MentorResponse>> GetFollowingMentorsAsync(Guid userId, int limit, int offset)
    {
        var mentors = await _unitOfWork.UserFollowsMentor.GetFollowedMentorsWithPaginationAsync(userId, limit, offset);
        var mentorList = mentors.ToList();
        
        var mentorResponses = mentorList.Select(m => _mapper.Map<MentorResponse>(m)).ToList();
        
        // Set IsFollowing flag (all mentors in this list are being followed)
        foreach (var mentor in mentorResponses)
        {
            mentor.IsFollowing = true;
        }

        // Check if there are more records
        var followedMentorIds = await _unitOfWork.UserFollowsMentor.GetFollowedMentorIdsAsync(userId);
        var followedMentorsCount = followedMentorIds.Count();
        var hasMore = (offset + limit) < followedMentorsCount;

        return new PagedResponse<MentorResponse>
        {
            Items = mentorResponses,
            Total = mentorResponses.Count,
            HasMore = hasMore,
            Limit = limit,
            Offset = offset
        };
    }
}
