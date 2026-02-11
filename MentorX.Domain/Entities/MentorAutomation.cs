namespace MentorX.Domain.Entities;

public class MentorAutomation
{
    public Guid MentorId { get; set; }
    public bool Enabled { get; set; } = false;
    public string Cadence { get; set; } = "daily";
    public string Timezone { get; set; } = "UTC";
    public DateTime? NextPostAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public Mentor Mentor { get; set; } = null!;
}
