namespace MentorX.Application.DTOs.Responses;

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public bool HasMore { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
}
