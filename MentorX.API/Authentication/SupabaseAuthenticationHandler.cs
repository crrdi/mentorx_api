using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;

namespace MentorX.API.Authentication;

public class SupabaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public SupabaseAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ExtractToken();
        
        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            // Token is a Supabase JWT (access_token) – decode locally and çıkar user id/email
            var payload = DecodeJwtPayload(token);

            if (!payload.TryGetValue("sub", out var subObj) || subObj is null)
                return Task.FromResult(AuthenticateResult.Fail("Invalid token: missing sub"));

            var userId = subObj.ToString() ?? string.Empty;
            payload.TryGetValue("email", out var emailObj);
            var email = emailObj?.ToString() ?? string.Empty;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Token validation failed");
            return Task.FromResult(AuthenticateResult.Fail("Invalid or expired token"));
        }
    }

    private string? ExtractToken()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return null;
        }

        return authHeader.Substring("Bearer ".Length).Trim();
    }

    private static Dictionary<string, object> DecodeJwtPayload(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
            throw new ArgumentException("Invalid JWT format");

        var payload = parts[1];
        // Base64 padding
        var padding = payload.Length % 4;
        if (padding > 0)
        {
            payload += new string('=', 4 - padding);
        }

        var jsonBytes = Convert.FromBase64String(payload);
        var json = Encoding.UTF8.GetString(jsonBytes);

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        return dict ?? new Dictionary<string, object>();
    }
}
