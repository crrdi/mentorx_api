using MentorX.Application.DTOs.Responses;

namespace MentorX.Application.Interfaces;

public interface IRevenueCatApiService
{
    Task<RevenueCatCustomerInfo?> GetCustomerInfoAsync(string appUserId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Verifies the purchase. When transactionId is null/empty, finds the latest purchase for productId from RevenueCat.
    /// Returns (verified, resolvedTransactionId, verifiedProductId). Use verifiedProductId for package lookup when present.
    /// </summary>
    Task<(bool Verified, string? ResolvedTransactionId, string? VerifiedProductId)> VerifyTransactionAsync(string appUserId, string? transactionId, string productId, CancellationToken cancellationToken = default);
}
