namespace MentorX.Domain.Entities;

public class CreditPackage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Credits { get; set; }
    public decimal Price { get; set; }
    public int? BonusPercentage { get; set; }
    public string? Badge { get; set; }
    /// <summary>RevenueCat store product id (e.g. com.erdiacar.mentorx.credits_100) for package lookup.</summary>
    public string? RevenueCatProductId { get; set; }
    /// <summary>RevenueCat package identifier (e.g. $rc_credits_100) when API returns this instead of store product id.</summary>
    public string? RevenueCatPackageId { get; set; }
    /// <summary>Package type: one_time or subscription.</summary>
    public string Type { get; set; } = "one_time";
}
