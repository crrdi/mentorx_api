using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.Interfaces;
using System.Security.Claims;

namespace MentorX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MentorsController : ControllerBase
{
    private readonly IMentorService _mentorService;
    private readonly ILogger<MentorsController> _logger;

    public MentorsController(IMentorService mentorService, ILogger<MentorsController> logger)
    {
        _mentorService = mentorService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetMentors(
        [FromQuery] string? tag,
        [FromQuery] bool popular = false,
        [FromQuery] bool followed = false,
        [FromQuery] string? search = null,
        [FromQuery] int limit = 5,
        [FromQuery] int offset = 0)
    {
        var userId = GetCurrentUserId();

        if (followed && userId == null)
        {
            return Unauthorized(new { error = "Authentication required to get followed mentors" });
        }

        var result = await _mentorService.GetMentorsAsync(tag, popular, followed, search, limit, offset, userId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMentorById(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorService.GetMentorByIdAsync(id, userId);

        if (result == null)
        {
            return NotFound(new { error = "Mentor not found" });
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateMentor([FromBody] CreateMentorRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var result = await _mentorService.CreateMentorAsync(userId.Value, request);
            return CreatedAtAction(nameof(GetMentorById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create mentor for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to create mentor. Please check your input and try again." });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateMentor(Guid id, [FromBody] UpdateMentorRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var result = await _mentorService.UpdateMentorAsync(id, userId.Value, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update mentor {MentorId} for user {UserId}", id, userId);
            return StatusCode(500, new { error = "Failed to update mentor. Please try again." });
        }
    }

    [HttpPost("{id}/follow")]
    [Authorize]
    public async Task<IActionResult> FollowMentor(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var result = await _mentorService.FollowMentorAsync(id, userId.Value);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to follow mentor {MentorId} for user {UserId}", id, userId);
            return StatusCode(500, new { error = "Failed to follow mentor. Please try again." });
        }
    }

    [HttpDelete("{id}/follow")]
    [Authorize]
    public async Task<IActionResult> UnfollowMentor(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var result = await _mentorService.UnfollowMentorAsync(id, userId.Value);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unfollow mentor {MentorId} for user {UserId}", id, userId);
            return StatusCode(500, new { error = "Failed to unfollow mentor. Please try again." });
        }
    }

    [HttpGet("{id}/replies")]
    public async Task<IActionResult> GetMentorReplies(Guid id)
    {
        try
        {
            var result = await _mentorService.GetMentorRepliesAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch replies for mentor {MentorId}", id);
            return StatusCode(500, new { error = "Failed to fetch mentor replies. Please try again." });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
    }
}
