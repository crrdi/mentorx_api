using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.Interfaces;
using System.Security.Claims;

namespace MentorX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ISubscriptionService subscriptionService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var result = await _userService.GetCurrentUserAsync(userId.Value);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var result = await _userService.UpdateUserAsync(userId.Value, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to update profile. Please try again." });
        }
    }

    [HttpGet("me/created-mentors")]
    public async Task<IActionResult> GetCreatedMentors([FromQuery] int limit = 10, [FromQuery] int offset = 0)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        if (limit < 1 || limit > 100)
        {
            return BadRequest(new { error = "Limit must be between 1 and 100" });
        }

        if (offset < 0)
        {
            return BadRequest(new { error = "Offset must be non-negative" });
        }

        try
        {
            var result = await _userService.GetCreatedMentorsAsync(userId.Value, limit, offset);
            return Ok(new
            {
                mentors = result.Items,
                hasMore = result.HasMore,
                offset = result.Offset,
                limit = result.Limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch created mentors for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to fetch your mentors. Please try again." });
        }
    }

    [HttpPost("me/revenuecat-customer")]
    public async Task<IActionResult> LinkRevenueCatCustomer([FromBody] LinkRevenueCatCustomerRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.CustomerId))
        {
            return BadRequest(new { error = "CustomerId is required" });
        }

        var (success, error) = await _subscriptionService.LinkRevenueCatCustomerAsync(userId.Value, request);
        if (!success)
        {
            return error == "User not found" ? NotFound(new { error }) : BadRequest(new { error });
        }

        return Ok(new { success = true, customerId = request.CustomerId.Trim() });
    }

    [HttpGet("me/subscription")]
    public async Task<IActionResult> GetSubscription()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        var subscription = await _subscriptionService.GetSubscriptionAsync(userId.Value);
        if (subscription == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(subscription);
    }

    [HttpGet("me/following-mentors")]
    public async Task<IActionResult> GetFollowingMentors([FromQuery] int limit = 10, [FromQuery] int offset = 0)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        if (limit < 1 || limit > 100)
        {
            return BadRequest(new { error = "Limit must be between 1 and 100" });
        }

        if (offset < 0)
        {
            return BadRequest(new { error = "Offset must be non-negative" });
        }

        try
        {
            var result = await _userService.GetFollowingMentorsAsync(userId.Value, limit, offset);
            return Ok(new
            {
                mentors = result.Items,
                hasMore = result.HasMore,
                offset = result.Offset,
                limit = result.Limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch following mentors for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to fetch followed mentors. Please try again." });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
    }
}
