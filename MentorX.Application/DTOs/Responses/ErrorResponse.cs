namespace MentorX.Application.DTOs.Responses;

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Details { get; set; }
    public string? StackTrace { get; set; }
}
