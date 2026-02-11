using MentorX.Domain.Entities;
using MentorX.Domain.Enums;

namespace MentorX.Domain.Interfaces;

public interface IActorRepository : IRepository<Actor>
{
    Task<Actor?> GetByUserIdAsync(Guid userId);
    Task<Actor?> GetByMentorIdAsync(Guid mentorId);
    Task<Actor?> GetByUserOrMentorIdAsync(Guid? userId, Guid? mentorId, ActorType type);
}
