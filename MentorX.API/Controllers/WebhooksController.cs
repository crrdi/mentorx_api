using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.Interfaces;
using System.Text.Json;

namespace MentorX.API.Controllers;

/// <summary>
/// Webhook endpoints. RevenueCat webhook does not use standard Bearer auth - it uses a shared secret in Authorization header.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IRevenueCatWebhookService _revenueCatService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IRevenueCatWebhookService revenueCatService, IConfiguration configuration, ILogger<WebhooksController> logger)
    {
        _revenueCatService = revenueCatService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// RevenueCat webhook endpoint. Validates Authorization Bearer token against REVENUECAT_WEBHOOK_SECRET.
    /// RevenueCat does not use cryptographic signatures - configure the same secret in RevenueCat dashboard as Authorization header.
    /// </summary>
    [HttpPost("revenuecat")]
    [AllowAnonymous]
    public async Task<IActionResult> RevenueCat([FromBody] RevenueCatWebhookRequest? request)
    {
        var webhookSecret = _configuration["RevenueCat:WebhookSecret"] ?? Environment.GetEnvironmentVariable("REVENUECAT_WEBHOOK_SECRET");
        if (string.IsNullOrEmpty(webhookSecret))
        {
            _logger.LogError("[RevenueCat Webhook] REVENUECAT_WEBHOOK_SECRET is not configured");
            return StatusCode(500, new { success = false, error = "Webhook not configured" });
        }

        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        var expectedAuth = $"Bearer {webhookSecret}";
        if (string.IsNullOrEmpty(authHeader) || authHeader != expectedAuth)
        {
            _logger.LogWarning("[RevenueCat Webhook] Invalid or missing Authorization header");
            return Unauthorized(new { success = false, error = "Invalid webhook signature" });
        }

        if (request == null)
        {
            return BadRequest(new { success = false, error = "Invalid request body" });
        }

        try
        {
            var (success, processed, error) = await _revenueCatService.ProcessWebhookAsync(request);

            if (!success)
            {
                return error?.Contains("User not found") == true
                    ? NotFound(new { success = false, processed = false, error })
                    : BadRequest(new { success = false, processed = false, error });
            }

            return Ok(new { success = true, processed });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RevenueCat Webhook] Error processing webhook");
            return StatusCode(500, new { success = false, processed = false, error = "Internal server error" });
        }
    }
}
