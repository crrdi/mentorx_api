using MentorX.Domain.Entities;

namespace MentorX.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    /// <summary>
    /// Finds user by app_user_id, original_app_user_id, or aliases (typically User.Id as Guid).
    /// RevenueCat recommends using your User.Id as app_user_id when configuring the SDK.
    /// </summary>
    Task<User?> GetByRevenueCatAppUserIdsAsync(string? appUserId, string? originalAppUserId, IReadOnlyList<string>? aliases);
}
