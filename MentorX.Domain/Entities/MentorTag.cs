namespace MentorX.Domain.Entities;

public class MentorTag
{
    public Guid MentorId { get; set; }
    public Guid TagId { get; set; }

    public Mentor Mentor { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
