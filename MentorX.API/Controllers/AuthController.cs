using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.Interfaces;

namespace MentorX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.IdToken))
            {
                _logger.LogWarning("Google auth request received with empty IdToken");
                return BadRequest(new { 
                    error = "Google token is missing. Please try signing in again.",
                    code = "GOOGLE_TOKEN_MISSING"
                });
            }

            var result = await _authService.GoogleAuthAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Google auth unauthorized: {Message}", ex.Message);
            return Unauthorized(new { 
                error = ex.Message,
                code = "GOOGLE_AUTH_FAILED"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Google auth bad request: {Message}", ex.Message);
            return BadRequest(new { 
                error = ex.Message,
                code = "GOOGLE_AUTH_INVALID_REQUEST"
            });
        }
        catch (Exception ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "Google auth failed: {Message}", message);
            return BadRequest(new { 
                error = $"An error occurred while signing in with Google: {message}",
                code = "GOOGLE_AUTH_ERROR"
            });
        }
    }

    /// <summary>
    /// Test email login - sadece hardcoded credentials çalışır
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> EmailLogin([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.EmailLoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Invalid email or password", code = "LOGIN_FAILED" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email login failed");
            return BadRequest(new { error = "Login failed", code = "LOGIN_ERROR" });
        }
    }

    [HttpPost("apple")]
    public async Task<IActionResult> AppleAuth([FromBody] AppleAuthRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.IdToken))
            {
                return BadRequest(new { error = "IdToken is required" });
            }

            var result = await _authService.AppleAuthAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Apple auth unauthorized");
            return Unauthorized(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Apple auth bad request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "Apple auth failed: {Message}", message);
            return BadRequest(new { error = message });
        }
    }
}
