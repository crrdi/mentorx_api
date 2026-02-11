namespace MentorX.Domain.Entities;

public class MentorRole : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    
    // Navigation property
    public List<Mentor> Mentors { get; set; } = new();
}
