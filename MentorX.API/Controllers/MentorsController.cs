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

    public MentorsController(IMentorService mentorService)
    {
        _mentorService = mentorService;
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
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
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
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
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
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
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
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
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
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
    }
}
