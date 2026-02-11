using MentorX.Domain.Entities;

namespace MentorX.Domain.Interfaces;

public interface IMentorRepository : IRepository<Mentor>
{
    Task<IEnumerable<Mentor>> GetByTagAsync(string tag);
    Task<IEnumerable<Mentor>> SearchAsync(string searchTerm);
    Task<IEnumerable<Mentor>> GetPopularAsync(int limit, int offset);
    Task<IEnumerable<Mentor>> GetByCreatorAsync(Guid userId);
    Task<bool> IsOwnerAsync(Guid mentorId, Guid userId);
    Task<IEnumerable<Mentor>> GetFollowedByUserAsync(Guid userId, int limit, int offset);
    Task<IEnumerable<Mentor>> GetByCreatorWithPaginationAsync(Guid userId, int limit, int offset);
}
