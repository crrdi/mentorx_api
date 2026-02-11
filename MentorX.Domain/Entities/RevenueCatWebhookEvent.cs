namespace MentorX.Domain.Entities;

/// <summary>
/// Tracks processed RevenueCat webhook events for idempotency.
/// Prevents duplicate processing when RevenueCat retries webhook delivery.
/// </summary>
public class RevenueCatWebhookEvent
{
    public string EventId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
