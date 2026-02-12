using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.Interfaces;
using System.Security.Claims;

namespace MentorX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CreditsController : ControllerBase
{
    private readonly ICreditService _creditService;

    public CreditsController(ICreditService creditService)
    {
        _creditService = creditService;
    }

    [HttpGet("packages")]
    public async Task<IActionResult> GetPackages()
    {
        var result = await _creditService.GetPackagesAsync();
        return Ok(result);
    }

    [HttpGet("balance")]
    [Authorize]
    public async Task<IActionResult> GetBalance()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var balance = await _creditService.GetBalanceAsync(userId.Value);
            return Ok(new { credits = balance });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("purchase")]
    [Authorize]
    public async Task<IActionResult> PurchaseCredits([FromBody] PurchaseCreditsRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        try
        {
            var result = await _creditService.PurchaseCreditsAsync(userId.Value, request);
            return Ok(result);
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

    [HttpPost("purchase-revenuecat")]
    [Authorize]
    public async Task<IActionResult> PurchaseCreditsFromRevenueCat([FromBody] VerifyRevenueCatPurchaseRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        if (string.IsNullOrEmpty(request.TransactionId))
        {
            return BadRequest(new { error = "TransactionId is required" });
        }

        if (string.IsNullOrEmpty(request.ProductId))
        {
            return BadRequest(new { error = "ProductId is required" });
        }

        try
        {
            var result = await _creditService.PurchaseCreditsFromRevenueCatAsync(userId.Value, request);
            
            if (!result.Success)
            {
                if (result.Verified && !string.IsNullOrEmpty(result.Error))
                {
                    // Transaction verified but package not found
                    return BadRequest(result);
                }
                // Transaction verification failed
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
    }
}
