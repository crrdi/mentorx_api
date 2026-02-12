using MentorX.Application.DTOs.Responses;

namespace MentorX.Application.Interfaces;

public interface IRevenueCatApiService
{
    Task<RevenueCatCustomerInfo?> GetCustomerInfoAsync(string appUserId, CancellationToken cancellationToken = default);
    Task<bool> VerifyTransactionAsync(string appUserId, string transactionId, string productId, CancellationToken cancellationToken = default);
}
