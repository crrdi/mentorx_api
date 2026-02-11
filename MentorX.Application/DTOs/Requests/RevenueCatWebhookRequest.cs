using System.Text.Json.Serialization;

namespace MentorX.Application.DTOs.Requests;

/// <summary>
/// RevenueCat webhook payload. Official format: { "event": {...}, "api_version": "1.0" }
/// See: https://www.revenuecat.com/docs/integrations/webhooks/event-types-and-fields
/// </summary>
public class RevenueCatWebhookRequest
{
    [JsonPropertyName("event")]
    public RevenueCatWebhookEventPayload Event { get; set; } = null!;

    [JsonPropertyName("api_version")]
    public string? ApiVersion { get; set; }
}

public class RevenueCatWebhookEventPayload
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("app_user_id")]
    public string? AppUserId { get; set; }

    [JsonPropertyName("original_app_user_id")]
    public string? OriginalAppUserId { get; set; }

    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }

    [JsonPropertyName("product_id")]
    public string? ProductId { get; set; }

    [JsonPropertyName("expiration_at_ms")]
    public long? ExpirationAtMs { get; set; }

    [JsonPropertyName("purchased_at_ms")]
    public long? PurchasedAtMs { get; set; }

    [JsonPropertyName("period_type")]
    public string? PeriodType { get; set; }

    [JsonPropertyName("environment")]
    public string? Environment { get; set; }

    [JsonPropertyName("entitlement_ids")]
    public List<string>? EntitlementIds { get; set; }

    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    [JsonPropertyName("transferred_from")]
    public List<string>? TransferredFrom { get; set; }

    [JsonPropertyName("transferred_to")]
    public List<string>? TransferredTo { get; set; }

    [JsonPropertyName("new_product_id")]
    public string? NewProductId { get; set; }
}
