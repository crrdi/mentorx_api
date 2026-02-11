namespace MentorX.Application.DTOs.Responses;

public class CreditPackageResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Credits { get; set; }
    public decimal Price { get; set; }
    public int? BonusPercentage { get; set; }
    public string? Badge { get; set; }
    /// <summary>RevenueCat product ID for in-app purchase (Flutter SDK).</summary>
    public string? RevenueCatProductId { get; set; }
    /// <summary>Package type: "one_time" or "subscription".</summary>
    public string Type { get; set; } = "one_time";
}
