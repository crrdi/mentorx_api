namespace MentorX.Application.DTOs.Requests;

public class VerifyRevenueCatPurchaseRequest
{
    public string TransactionId { get; set; } = string.Empty; // Store transaction ID
    public string ProductId { get; set; } = string.Empty;   // RevenueCat product ID
    public string? PackageId { get; set; }                    // Backend package ID (optional)
}
