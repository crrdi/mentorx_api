using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace MentorX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InsightsController : ControllerBase
{
    private readonly IInsightService _insightService;
    private readonly ILogger<InsightsController> _logger;

    public InsightsController(IInsightService insightService, ILogger<InsightsController> logger)
    {
        _insightService = insightService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetInsights(
        [FromQuery] string? tag,
        [FromQuery] Guid? mentorId,
        [FromQuery] string? mentorIds,
        [FromQuery] string? sort = "latest",
        [FromQuery] int limit = 5,
        [FromQuery] int offset = 0)
    {
        _logger.LogInformation("[GetInsights] Request - tag: {Tag}, mentorId: {MentorId}, mentorIds: {MentorIds}, sort: {Sort}, limit: {Limit}, offset: {Offset}",
            tag, mentorId, mentorIds, sort, limit, offset);

        var userId = GetCurrentUserId();
        var result = await _insightService.GetInsightsAsync(tag, mentorId, mentorIds, sort, limit, offset, userId);
        
        _logger.LogInformation("[GetInsights] Response - Total: {Total}, Items Count: {ItemsCount}, HasMore: {HasMore}",
            result.Total, result.Items.Count, result.HasMore);
        
        if (result.Items.Any())
        {
            _logger.LogInformation("[GetInsights] First Insight ID: {FirstInsightId}, Tags: {Tags}",
                result.Items.First().Id, JsonSerializer.Serialize(result.Items.First().Tags));
        }
        else
        {
            _logger.LogWarning("[GetInsights] No insights returned for tag: {Tag}", tag);
        }

        return Ok(new
        {
            insights = result.Items,
            total = result.Total,
            hasMore = result.HasMore,
            limit = result.Limit,
            offset = result.Offset
        });
    }

    /// <summary>Literal "feed" route must be registered before "{id}" so GET /api/insights/feed is not matched as id.</summary>
    [HttpGet("feed")]
    [Authorize]
    public async Task<IActionResult> GetFeed(
        [FromQuery] string? tag,
        [FromQuery] int limit = 5,
        [FromQuery] int offset = 0)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var result = await _insightService.GetFeedAsync(userId.Value, tag, limit, offset);
            return Ok(new
            {
                insights = result.Items,
                total = result.Total,
                hasMore = result.HasMore,
                limit = result.Limit,
                offset = result.Offset
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch feed");
            return StatusCode(500, new { error = "Failed to fetch feed. Please try again." });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetInsightById(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _insightService.GetInsightByIdAsync(id, userId);
        
        if (result == null)
        {
            return NotFound(new { error = "Insight not found" });
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateInsight([FromBody] CreateInsightRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            // Check if it's a thread request
            if (request.IsThread)
            {
                var result = await _insightService.CreateThreadAsync(userId.Value, request);
                return Ok(new { insights = result });
            }
            else
            {
                var result = await _insightService.CreateInsightAsync(userId.Value, request);
                return CreatedAtAction(nameof(GetInsightById), new { id = result.Id }, result);
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("credits"))
        {
            return StatusCode(402, new { error = ex.Message, code = "INSUFFICIENT_CREDITS" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create insight");
            return StatusCode(500, new { error = "Failed to create insight. Please try again." });
        }
    }

    [HttpPost("thread")]
    [Authorize]
    public async Task<IActionResult> CreateThread([FromBody] CreateInsightRequest request)
    {
        // Legacy endpoint - redirects to main CreateInsight with IsThread flag
        request.IsThread = true;
        return await CreateInsight(request);
    }

    [HttpPost("{id}/like")]
    [Authorize]
    public async Task<IActionResult> LikeInsight(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var result = await _insightService.LikeInsightAsync(userId.Value, id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to like insight {InsightId}", id);
            return StatusCode(500, new { error = "Failed to like insight. Please try again." });
        }
    }

    [HttpDelete("{id}/like")]
    [Authorize]
    public async Task<IActionResult> UnlikeInsight(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var result = await _insightService.UnlikeInsightAsync(userId.Value, id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unlike insight {InsightId}", id);
            return StatusCode(500, new { error = "Failed to unlike insight. Please try again." });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
    }
}
