using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace MentorX.API.Controllers;

[ApiController]
[Route("api/insights/{insightId}/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(ICommentService commentService, ILogger<CommentsController> logger)
    {
        _commentService = commentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetComments(
        Guid insightId,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        try
        {
            var result = await _commentService.GetCommentsByInsightIdAsync(insightId, limit, offset);
            return Ok(new
            {
                comments = result.Items,
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
    [Authorize]
    public async Task<IActionResult> CreateComment(Guid insightId, [FromBody] CreateCommentRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("CreateComment: Unauthorized - userId is null");
            return Unauthorized(new { error = "Unauthorized" });
        }

        // Log request
        var requestJson = JsonSerializer.Serialize(new
        {
            insightId,
            userId = userId.Value,
            request = new
            {
                content = request.Content,
                parentId = request.ParentId,
                mentorId = request.MentorId
            }
        });
        _logger.LogInformation("CreateComment Request: {RequestJson}", requestJson);

        try
        {
            var result = await _commentService.CreateCommentAsync(insightId, userId.Value, request);
            
            // Log response
            var responseJson = JsonSerializer.Serialize(new
            {
                id = result.Id,
                insightId = result.InsightId,
                content = result.Content,
                author = result.Author != null ? new { id = result.Author.Id, name = result.Author.Name, type = result.Author.Type } : null,
                parentId = result.ParentId,
                createdAt = result.CreatedAt
            });
            _logger.LogInformation("CreateComment Response (201 Created): {ResponseJson}", responseJson);
            
            return CreatedAtAction(nameof(GetComments), new { insightId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("CreateComment: NotFound - {ErrorMessage}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("CreateComment: Forbidden - {ErrorMessage}", ex.Message);
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateComment: BadRequest - {ErrorMessage}", ex.Message);
            
            // Log error response
            var errorResponseJson = JsonSerializer.Serialize(new { error = ex.Message });
            _logger.LogError("CreateComment Error Response (400 BadRequest): {ErrorResponseJson}", errorResponseJson);
            
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
    }
}
