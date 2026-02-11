namespace MentorX.Application.DTOs.Responses;

public class SubscriptionResponse
{
    public string Status { get; set; } = string.Empty;
    public string? ProductId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? CustomerId { get; set; }
}
