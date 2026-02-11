using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;

namespace MentorX.Application.Interfaces;

public interface ISubscriptionService
{
    Task<(bool Success, string? Error)> LinkRevenueCatCustomerAsync(Guid userId, LinkRevenueCatCustomerRequest request, CancellationToken cancellationToken = default);
    Task<SubscriptionResponse?> GetSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);
}
