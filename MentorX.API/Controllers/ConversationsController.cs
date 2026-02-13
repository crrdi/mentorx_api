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
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(IConversationService conversationService, ILogger<ConversationsController> logger)
    {
        _conversationService = conversationService;
        _logger = logger;
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
            _logger.LogError(ex, "Failed to fetch conversations");
            return StatusCode(500, new { error = "Failed to fetch conversations. Please try again." });
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create conversation");
            return StatusCode(500, new { error = "Failed to create conversation. Please try again." });
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
            _logger.LogError(ex, "Failed to fetch messages for conversation {ConversationId}", id);
            return StatusCode(500, new { error = "Failed to fetch messages. Please try again." });
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message in conversation {ConversationId}", id);
            return StatusCode(500, new { error = "Failed to send message. Please try again." });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
    }
}
