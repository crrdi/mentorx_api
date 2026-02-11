using Microsoft.Extensions.Logging;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.Interfaces;
using MentorX.Domain.Entities;
using MentorX.Domain.Enums;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.DbContext;
using MentorX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace MentorX.Infrastructure.Services;

public class RevenueCatWebhookService : IRevenueCatWebhookService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly MentorXDbContext _dbContext;
    private readonly ILogger<RevenueCatWebhookService> _logger;

    public RevenueCatWebhookService(IUnitOfWork unitOfWork, MentorXDbContext dbContext, ILogger<RevenueCatWebhookService> logger)
    {
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(bool Success, bool Processed, string? Error)> ProcessWebhookAsync(RevenueCatWebhookRequest request, CancellationToken cancellationToken = default)
    {
        if (request?.Event == null)
        {
            return (false, false, "Invalid webhook payload: event is required");
        }

        var evt = request.Event;
        _logger.LogInformation("[RevenueCat Webhook] Event: {EventType}, Id: {EventId}, Product: {ProductId}, AppUserId: {AppUserId}",
            evt.Type, evt.Id, evt.ProductId, evt.AppUserId ?? "(null)");

        var alreadyProcessed = await _dbContext.RevenueCatWebhookEvents.AnyAsync(e => e.EventId == evt.Id, cancellationToken);
        if (alreadyProcessed)
        {
            _logger.LogInformation("[RevenueCat Webhook] Event {EventId} already processed, skipping", evt.Id);
            return (true, false, null);
        }

        if (evt.Type == "TRANSFER")
        {
            return await HandleTransferAsync(evt, cancellationToken);
        }

        var user = await _unitOfWork.Users.GetByRevenueCatAppUserIdsAsync(
            evt.AppUserId, evt.OriginalAppUserId, evt.Aliases);

        if (user == null)
        {
            _logger.LogWarning("[RevenueCat Webhook] User not found for app_user_id: {AppUserId}, original: {Original}, aliases: {Aliases}",
                evt.AppUserId, evt.OriginalAppUserId, evt.Aliases != null ? string.Join(",", evt.Aliases) : "null");
            return (false, false, "User not found. Ensure app_user_id is set to your User.Id (Guid) in RevenueCat SDK.");
        }

        var expirationAt = evt.ExpirationAtMs.HasValue && evt.ExpirationAtMs > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(evt.ExpirationAtMs.Value).UtcDateTime
            : (DateTime?)null;

        switch (evt.Type)
        {
            case "INITIAL_PURCHASE":
            case "RENEWAL":
            case "NON_RENEWING_PURCHASE":
                await HandlePurchaseAsync(user, evt, expirationAt, cancellationToken);
                break;
            case "CANCELLATION":
                await HandleCancellationAsync(user, cancellationToken);
                break;
            case "UNCANCELLATION":
                await HandleUncancellationAsync(user, evt, expirationAt, cancellationToken);
                break;
            case "EXPIRATION":
                await HandleExpirationAsync(user, cancellationToken);
                break;
            case "BILLING_ISSUE":
                _logger.LogWarning("[RevenueCat Webhook] BILLING_ISSUE for user {UserId} - consider sending notification", user.Id);
                break;
            case "SUBSCRIPTION_PAUSED":
                user.SubscriptionStatus = "paused";
                await _unitOfWork.Users.UpdateAsync(user);
                break;
            case "SUBSCRIPTION_EXTENDED":
            case "PRODUCT_CHANGE":
                user.SubscriptionExpiresAt = expirationAt;
                if (!string.IsNullOrEmpty(evt.NewProductId ?? evt.ProductId))
                    user.SubscriptionProductId = evt.NewProductId ?? evt.ProductId;
                await _unitOfWork.Users.UpdateAsync(user);
                break;
            case "REFUND_REVERSED":
                if (!string.IsNullOrEmpty(evt.ProductId))
                {
                    var refundPackage = (await _unitOfWork.CreditPackages.FindAsync(p => p.RevenueCatProductId == evt.ProductId)).FirstOrDefault();
                    if (refundPackage != null)
                    {
                        await AddCreditsAsync(user, refundPackage.Credits, evt.TransactionId ?? evt.Id, cancellationToken);
                    }
                }
                break;
            case "TEST":
                _logger.LogInformation("[RevenueCat Webhook] TEST event received and acknowledged");
                break;
            default:
                _logger.LogInformation("[RevenueCat Webhook] Unhandled event type: {EventType}", evt.Type);
                break;
        }

        _dbContext.RevenueCatWebhookEvents.Add(new RevenueCatWebhookEvent
        {
            EventId = evt.Id,
            ProcessedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (true, true, null);
    }

    private async Task<(bool Success, bool Processed, string? Error)> HandleTransferAsync(
        MentorX.Application.DTOs.Requests.RevenueCatWebhookEventPayload evt, CancellationToken cancellationToken)
    {
        if (evt.TransferredTo == null || evt.TransferredTo.Count == 0)
        {
            return (true, false, null);
        }

        foreach (var toUserId in evt.TransferredTo)
        {
            if (!Guid.TryParse(toUserId, out var toGuid)) continue;
            var user = await _unitOfWork.Users.GetByIdAsync(toGuid);
            if (user == null) continue;
            _logger.LogInformation("[RevenueCat Webhook] TRANSFER to user {UserId} acknowledged", toGuid);
        }

        _dbContext.RevenueCatWebhookEvents.Add(new RevenueCatWebhookEvent
        {
            EventId = evt.Id,
            ProcessedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (true, true, null);
    }

    private async Task HandlePurchaseAsync(User user, RevenueCatWebhookEventPayload evt, DateTime? expirationAt, CancellationToken cancellationToken)
    {
        var creditsToAdd = 0;
        if (!string.IsNullOrEmpty(evt.ProductId))
        {
            var package = (await _unitOfWork.CreditPackages.FindAsync(p => p.RevenueCatProductId == evt.ProductId)).FirstOrDefault();
            if (package != null)
            {
                creditsToAdd = package.Credits;
                await AddCreditsAsync(user, creditsToAdd, evt.TransactionId ?? evt.Id, cancellationToken);
            }
        }

        user.SubscriptionStatus = "active";
        user.SubscriptionProductId = evt.ProductId;
        user.SubscriptionExpiresAt = expirationAt;

        await _unitOfWork.Users.UpdateAsync(user);
    }

    private async Task HandleCancellationAsync(User user, CancellationToken cancellationToken)
    {
        user.SubscriptionStatus = "cancelled";
        await _unitOfWork.Users.UpdateAsync(user);
    }

    private async Task HandleUncancellationAsync(User user, RevenueCatWebhookEventPayload evt, DateTime? expirationAt, CancellationToken cancellationToken)
    {
        user.SubscriptionStatus = "active";
        user.SubscriptionExpiresAt = expirationAt;
        if (!string.IsNullOrEmpty(evt.ProductId))
            user.SubscriptionProductId = evt.ProductId;
        await _unitOfWork.Users.UpdateAsync(user);
    }

    private async Task HandleExpirationAsync(User user, CancellationToken cancellationToken)
    {
        user.SubscriptionStatus = "expired";
        await _unitOfWork.Users.UpdateAsync(user);
    }

    private async Task AddCreditsAsync(User user, int credits, string transactionId, CancellationToken cancellationToken)
    {
        user.Credits += credits;
        await _unitOfWork.Users.UpdateAsync(user);

        var transaction = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = CreditTransactionType.Purchase,
            Amount = credits,
            BalanceAfter = user.Credits,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.CreditTransactions.Add(transaction);
    }
}
