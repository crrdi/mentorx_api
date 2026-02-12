namespace MentorX.Application.DTOs.Responses;

public class VerifyRevenueCatPurchaseResponse
{
    public bool Success { get; set; }
    public bool Verified { get; set; }
    public int CreditsAdded { get; set; }
    public int NewBalance { get; set; }
    public string? Error { get; set; }
}
