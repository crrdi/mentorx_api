namespace MentorX.Domain.Entities;

public class CreditPackage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Credits { get; set; }
    public decimal Price { get; set; }
    public int? BonusPercentage { get; set; }
    public string? Badge { get; set; }
    /// <summary>RevenueCat store_identifier (e.g. com.erdiacar.mentorx.credits_100) for in-app purchase.</summary>
    public string? RevenueCatProductId { get; set; }
    /// <summary>Package type: one_time or subscription.</summary>
    public string Type { get; set; } = "one_time";
}
