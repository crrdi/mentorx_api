using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.Interfaces;
using System.Security.Claims;

namespace MentorX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;

    public ConversationsController(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var result = await _conversationService.GetConversationsAsync(userId.Value, limit, offset);
            return Ok(new
            {
                conversations = result.Items,
                total = result.Total,
                hasMore = result.HasMore,
                limit = result.Limit,
                offset = result.Offset
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var result = await _conversationService.CreateConversationAsync(userId.Value, request);
            return CreatedAtAction(nameof(GetConversations), result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid id,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var result = await _conversationService.GetMessagesAsync(id, userId.Value, limit, offset);
            return Ok(new
            {
                messages = result.Items,
                total = result.Total,
                hasMore = result.HasMore,
                limit = result.Limit,
                offset = result.Offset
            });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { error = "Conversation not found or access denied" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] CreateMessageRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var result = await _conversationService.SendMessageAsync(id, userId.Value, request);
            return CreatedAtAction(nameof(GetMessages), new { id }, result);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { error = "Conversation not found or access denied" });
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
