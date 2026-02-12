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
            return (false, false, "User not found. Ensure app_user_id is set to your User.Id (Guid) in RevenueCat SDK, or sync RevenueCat customer ID via POST /api/users/me/revenuecat-customer endpoint.");
        }

        // Update RevenueCatCustomerId if not set (for anonymous IDs or when linking customer ID)
        // Use app_user_id first, then original_app_user_id, then first alias
        var customerIdToSet = evt.AppUserId ?? evt.OriginalAppUserId ?? evt.Aliases?.FirstOrDefault();
        if (!string.IsNullOrEmpty(customerIdToSet) && string.IsNullOrEmpty(user.RevenueCatCustomerId))
        {
            user.RevenueCatCustomerId = customerIdToSet;
            _logger.LogInformation("[RevenueCat Webhook] Setting RevenueCatCustomerId to {CustomerId} for user {UserId}", 
                customerIdToSet, user.Id);
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
                // User entity is already tracked, changes will be saved when SaveChangesAsync is called
                break;
            case "SUBSCRIPTION_EXTENDED":
            case "PRODUCT_CHANGE":
                user.SubscriptionExpiresAt = expirationAt;
                if (!string.IsNullOrEmpty(evt.NewProductId ?? evt.ProductId))
                    user.SubscriptionProductId = evt.NewProductId ?? evt.ProductId;
                // User entity is already tracked, changes will be saved when SaveChangesAsync is called
                break;
            case "REFUND_REVERSED":
                if (!string.IsNullOrEmpty(evt.ProductId))
                {
                    var refundPackage = (await _unitOfWork.CreditPackages.FindAsync(p => p.RevenueCatProductId == evt.ProductId)).FirstOrDefault();
                    if (refundPackage != null)
                    {
                        _logger.LogInformation("[RevenueCat Webhook] REFUND_REVERSED: Adding {Credits} credits to user {UserId} for product {ProductId}", 
                            refundPackage.Credits, user.Id, evt.ProductId);
                        await AddCreditsAsync(user, refundPackage.Credits, evt.TransactionId ?? evt.Id, cancellationToken);
                    }
                    else
                    {
                        _logger.LogWarning("[RevenueCat Webhook] REFUND_REVERSED: Package not found for product {ProductId}. User {UserId} will not receive credits.", 
                            evt.ProductId, user.Id);
                    }
                }
                else
                {
                    _logger.LogWarning("[RevenueCat Webhook] REFUND_REVERSED: ProductId is empty. User {UserId} will not receive credits.", user.Id);
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
                _logger.LogInformation("[RevenueCat Webhook] Adding {Credits} credits to user {UserId} for product {ProductId}", 
                    creditsToAdd, user.Id, evt.ProductId);
                await AddCreditsAsync(user, creditsToAdd, evt.TransactionId ?? evt.Id, cancellationToken);
            }
            else
            {
                _logger.LogWarning("[RevenueCat Webhook] Package not found for product {ProductId}. User {UserId} will not receive credits.", 
                    evt.ProductId, user.Id);
            }
        }
        else
        {
            _logger.LogWarning("[RevenueCat Webhook] ProductId is empty for purchase event. User {UserId} will not receive credits.", user.Id);
        }

        user.SubscriptionStatus = "active";
        user.SubscriptionProductId = evt.ProductId;
        user.SubscriptionExpiresAt = expirationAt;

        // User entity is already tracked, no need to call UpdateAsync again
        // The changes will be saved when SaveChangesAsync is called
    }

    private async Task HandleCancellationAsync(User user, CancellationToken cancellationToken)
    {
        user.SubscriptionStatus = "cancelled";
        _logger.LogInformation("[RevenueCat Webhook] Subscription cancelled for user {UserId}", user.Id);
        // User entity is already tracked, changes will be saved when SaveChangesAsync is called
    }

    private async Task HandleUncancellationAsync(User user, RevenueCatWebhookEventPayload evt, DateTime? expirationAt, CancellationToken cancellationToken)
    {
        user.SubscriptionStatus = "active";
        user.SubscriptionExpiresAt = expirationAt;
        if (!string.IsNullOrEmpty(evt.ProductId))
            user.SubscriptionProductId = evt.ProductId;
        _logger.LogInformation("[RevenueCat Webhook] Subscription uncancelled for user {UserId}", user.Id);
        // User entity is already tracked, changes will be saved when SaveChangesAsync is called
    }

    private async Task HandleExpirationAsync(User user, CancellationToken cancellationToken)
    {
        user.SubscriptionStatus = "expired";
        _logger.LogInformation("[RevenueCat Webhook] Subscription expired for user {UserId}", user.Id);
        // User entity is already tracked, changes will be saved when SaveChangesAsync is called
    }

    private async Task AddCreditsAsync(User user, int credits, string transactionId, CancellationToken cancellationToken)
    {
        var oldCredits = user.Credits;
        user.Credits += credits;
        
        _logger.LogInformation("[RevenueCat Webhook] User {UserId} credits: {OldCredits} -> {NewCredits} (added {Credits})", 
            user.Id, oldCredits, user.Credits, credits);

        // User entity is already tracked by EF Core from GetByRevenueCatAppUserIdsAsync
        // No need to call UpdateAsync - changes will be saved when SaveChangesAsync is called

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
