namespace MentorX.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public List<string> FocusAreas { get; set; } = new();
    public int Credits { get; set; } = 10;
    
    // RevenueCat subscription fields
    public string? RevenueCatCustomerId { get; set; }
    public string? SubscriptionStatus { get; set; } // none, active, expired, cancelled, paused
    public string? SubscriptionProductId { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
}
