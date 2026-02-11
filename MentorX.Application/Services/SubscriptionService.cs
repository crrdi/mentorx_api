using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;
using MentorX.Application.Interfaces;
using MentorX.Domain.Interfaces;

namespace MentorX.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<(bool Success, string? Error)> LinkRevenueCatCustomerAsync(Guid userId, LinkRevenueCatCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            return (false, "CustomerId is required");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found");
        }

        user.RevenueCatCustomerId = request.CustomerId.Trim();
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return (true, null);
    }

    public async Task<SubscriptionResponse?> GetSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var isActive = user.SubscriptionStatus == "active"
            && user.SubscriptionExpiresAt.HasValue
            && user.SubscriptionExpiresAt.Value > DateTime.UtcNow;

        return new SubscriptionResponse
        {
            Status = user.SubscriptionStatus ?? "none",
            ProductId = user.SubscriptionProductId,
            ExpiresAt = user.SubscriptionExpiresAt,
            IsActive = isActive,
            CustomerId = user.RevenueCatCustomerId
        };
    }
}
