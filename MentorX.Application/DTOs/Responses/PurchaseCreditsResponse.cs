namespace MentorX.Application.DTOs.Responses;

public class PurchaseCreditsResponse
{
    public bool Success { get; set; }
    public int CreditsAdded { get; set; }
    public int NewBalance { get; set; }
    public UserResponse User { get; set; } = null!;
}
