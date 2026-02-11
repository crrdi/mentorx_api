using System.Net;
using System.Text.Json;
using MentorX.Application.DTOs.Responses;

namespace MentorX.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ErrorHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ErrorHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var errorCode = "INTERNAL_ERROR";
        var message = "An error occurred while processing your request.";

        switch (exception)
        {
            case KeyNotFoundException:
                code = HttpStatusCode.NotFound;
                errorCode = "NOT_FOUND";
                message = exception.Message;
                break;
            case UnauthorizedAccessException:
                code = HttpStatusCode.Unauthorized;
                errorCode = "UNAUTHORIZED";
                message = exception.Message;
                break;
            case InvalidOperationException when exception.Message.Contains("credits"):
                code = HttpStatusCode.PaymentRequired;
                errorCode = "INSUFFICIENT_CREDITS";
                message = exception.Message;
                break;
            case InvalidOperationException:
                code = HttpStatusCode.BadRequest;
                errorCode = "VALIDATION_ERROR";
                message = exception.Message;
                break;
            case ArgumentException:
                code = HttpStatusCode.BadRequest;
                errorCode = "VALIDATION_ERROR";
                message = exception.Message;
                break;
        }

        var response = new ErrorResponse
        {
            Error = message,
            Code = errorCode
        };

        // Include detailed error information in development
        if (_environment.IsDevelopment())
        {
            response.Details = exception.Message;
            response.StackTrace = exception.StackTrace;
            
            // Include inner exception details if present
            if (exception.InnerException != null)
            {
                response.Details += $"\n\nInner Exception: {exception.InnerException.Message}";
                if (exception.InnerException.StackTrace != null)
                {
                    response.StackTrace += $"\n\nInner Stack Trace:\n{exception.InnerException.StackTrace}";
                }
            }
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
