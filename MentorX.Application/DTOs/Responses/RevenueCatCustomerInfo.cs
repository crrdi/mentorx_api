namespace MentorX.Application.DTOs.Responses;

public class RevenueCatCustomerInfo
{
    public string? AppUserId { get; set; }
    public List<RevenueCatPurchase>? Purchases { get; set; }
}

public class RevenueCatPurchase
{
    /// <summary>RevenueCat internal id or first available transaction id.</summary>
    public string? TransactionId { get; set; }
    /// <summary>Store transaction id (e.g. Stripe o1_xxx) when present in API.</summary>
    public string? StoreTransactionId { get; set; }
    public string? ProductId { get; set; }
    public long? PurchasedAtMs { get; set; }
    public string? Store { get; set; }
}
