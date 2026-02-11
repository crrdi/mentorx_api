using MentorX.Domain.Enums;

namespace MentorX.Domain.Entities;

public class CreditTransaction : BaseEntity
{
    public Guid UserId { get; set; }
    public CreditTransactionType Type { get; set; }
    public int Amount { get; set; }
    public int BalanceAfter { get; set; }
    
    // Navigation property
    public User User { get; set; } = null!;
}
