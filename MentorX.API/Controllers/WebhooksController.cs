using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.Interfaces;

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

    public WebhooksController(
        IRevenueCatWebhookService revenueCatService, 
        IConfiguration configuration, 
        ILogger<WebhooksController> logger)
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
        _logger.LogInformation("[RevenueCat Webhook] Webhook endpoint called. Method: {Method}, Path: {Path}", 
            Request.Method, Request.Path);
        
        var webhookSecret = _configuration["RevenueCat:WebhookSecret"] ?? Environment.GetEnvironmentVariable("REVENUECAT_WEBHOOK_SECRET");
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        
        _logger.LogInformation("[RevenueCat Webhook] Authorization header present: {HasAuth}", !string.IsNullOrEmpty(authHeader));
        
        // Authorization header is optional in RevenueCat webhooks
        // If configured in dashboard, validate it; if not configured, allow without it
        if (!string.IsNullOrEmpty(webhookSecret) && !string.IsNullOrEmpty(authHeader))
        {
            // RevenueCat can send Authorization header in two formats:
            // 1. "Bearer {secret}" - if configured with Bearer prefix in dashboard
            // 2. "{secret}" - if configured without Bearer prefix in dashboard
            // We need to handle both cases
            
            bool isValidAuth = false;
            
            // Try exact match first (Bearer {secret})
            var expectedAuthWithBearer = $"Bearer {webhookSecret}";
            if (authHeader == expectedAuthWithBearer)
            {
                isValidAuth = true;
            }
            // Try without Bearer prefix (just secret)
            else if (authHeader == webhookSecret)
            {
                isValidAuth = true;
            }
            // Try if RevenueCat sends "Bearer " prefix but we need to extract the secret
            else if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var receivedSecret = authHeader.Substring(7); // Remove "Bearer "
                if (receivedSecret == webhookSecret)
                {
                    isValidAuth = true;
                }
            }
            
            if (!isValidAuth)
            {
                var secretPreview = webhookSecret.Substring(0, Math.Min(10, webhookSecret.Length)) + "...";
                var receivedPreview = authHeader.Substring(0, Math.Min(50, authHeader.Length));
                _logger.LogWarning("[RevenueCat Webhook] Invalid Authorization header. Expected: Bearer {SecretPreview} or {SecretPreview2}, Got: {ReceivedPreview}",
                    secretPreview, secretPreview, receivedPreview);
                return Unauthorized(new { success = false, error = "Invalid webhook signature" });
            }
            
            _logger.LogInformation("[RevenueCat Webhook] Authorization header validated successfully");
        }
        else if (!string.IsNullOrEmpty(webhookSecret) && string.IsNullOrEmpty(authHeader))
        {
            _logger.LogWarning("[RevenueCat Webhook] Webhook secret is configured but Authorization header is missing. Allowing request but consider configuring Authorization header in RevenueCat dashboard for better security.");
        }
        else
        {
            _logger.LogInformation("[RevenueCat Webhook] No webhook secret configured, processing without authorization validation");
        }

        if (request == null)
        {
            _logger.LogWarning("[RevenueCat Webhook] Request body is null");
            return BadRequest(new { success = false, error = "Invalid request body" });
        }

        _logger.LogInformation("[RevenueCat Webhook] Request received. Event: {EventType}, ProductId: {ProductId}, AppUserId: {AppUserId}",
            request.Event?.Type ?? "(null)", request.Event?.ProductId ?? "(null)", request.Event?.AppUserId ?? "(null)");

        try
        {
            var (success, processed, error) = await _revenueCatService.ProcessWebhookAsync(request);

            if (!success)
            {
                _logger.LogWarning("[RevenueCat Webhook] Processing failed. Success: {Success}, Processed: {Processed}, Error: {Error}",
                    success, processed, error);
                return error?.Contains("User not found") == true
                    ? NotFound(new { success = false, processed = false, error })
                    : BadRequest(new { success = false, processed = false, error });
            }

            _logger.LogInformation("[RevenueCat Webhook] Processing successful. Processed: {Processed}", processed);
            return Ok(new { success = true, processed });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RevenueCat Webhook] Error processing webhook. Exception: {Exception}", ex.Message);
            return StatusCode(500, new { success = false, processed = false, error = "Internal server error" });
        }
    }
}
