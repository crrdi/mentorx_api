using MentorX.Domain.Entities;

namespace MentorX.Domain.Interfaces;

public interface IUserLikesRepository
{
    Task<bool> IsLikedAsync(Guid userId, Guid insightId);
    Task LikeAsync(Guid userId, Guid insightId);
    Task UnlikeAsync(Guid userId, Guid insightId);
    Task<IEnumerable<Guid>> GetLikedInsightIdsAsync(Guid userId);
}
