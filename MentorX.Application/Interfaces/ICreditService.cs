using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;

namespace MentorX.Application.Interfaces;

public interface ICreditService
{
    Task<List<CreditPackageResponse>> GetPackagesAsync();
    Task<int> GetBalanceAsync(Guid userId);
    Task<PurchaseCreditsResponse> PurchaseCreditsAsync(Guid userId, PurchaseCreditsRequest request);
    Task<VerifyRevenueCatPurchaseResponse> PurchaseCreditsFromRevenueCatAsync(Guid userId, VerifyRevenueCatPurchaseRequest request);
}
