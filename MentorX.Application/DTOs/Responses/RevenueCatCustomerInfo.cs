namespace MentorX.Application.DTOs.Responses;

public class RevenueCatCustomerInfo
{
    public string? AppUserId { get; set; }
    public List<RevenueCatPurchase>? Purchases { get; set; }
}

public class RevenueCatPurchase
{
    public string? TransactionId { get; set; }
    public string? ProductId { get; set; }
    public long? PurchasedAtMs { get; set; }
    public string? Store { get; set; }
}
