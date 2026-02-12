using MentorX.Application.DTOs.Responses;

namespace MentorX.Application.Interfaces;

public interface IRevenueCatApiService
{
    Task<RevenueCatCustomerInfo?> GetCustomerInfoAsync(string appUserId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Verifies the purchase. When transactionId is null/empty, finds the latest purchase for productId from RevenueCat.
    /// Returns (verified, resolvedTransactionId). ResolvedTransactionId is used for idempotency.
    /// </summary>
    Task<(bool Verified, string? ResolvedTransactionId)> VerifyTransactionAsync(string appUserId, string? transactionId, string productId, CancellationToken cancellationToken = default);
}
