using MentorX.Domain.Entities;

namespace MentorX.Domain.Interfaces;

public interface IUserFollowsMentorRepository
{
    Task<bool> IsFollowingAsync(Guid userId, Guid mentorId);
    Task FollowAsync(Guid userId, Guid mentorId);
    Task UnfollowAsync(Guid userId, Guid mentorId);
    Task<IEnumerable<Guid>> GetFollowedMentorIdsAsync(Guid userId);
    Task<int> GetFollowerCountAsync(Guid mentorId);
    Task<IEnumerable<Mentor>> GetFollowedMentorsWithPaginationAsync(Guid userId, int limit, int offset);
}
