namespace MentorX.Domain.Entities;

public class InsightTag
{
    public Guid InsightId { get; set; }
    public Guid TagId { get; set; }

    public Insight Insight { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
