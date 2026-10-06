using System.Security.Cryptography;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Splendor.Api.Auth;

namespace Splendor.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IDocumentSession _session;
    private readonly IConfiguration _configuration;

    public AuthController(IDocumentSession session, IConfiguration configuration)
    {
        _session = session;
        _configuration = configuration;
    }

    public record GuestResponse(string Token, string DisplayName, DateTimeOffset ExpiresAt);

    /// <summary>
    /// Issues a short-lived token for an anonymous guest, up to Guest:MaxActive at a time.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting("GuestAuth")]
    [HttpPost("guest")]
    public async Task<IActionResult> CreateGuest(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var maxActive = _configuration.GetValue("Guest:MaxActive", 10);

        // ponytail: count-then-insert can overshoot the limit by a request or two under a race; use a DB constraint if it matters
        var active = await _session.Query<GuestSession>().CountAsync(g => g.ExpiresAt > now, cancellationToken);
        if (active >= maxActive)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Guest limit reached. Try again later." });
        }

        var expiresAt = now.AddMinutes(_configuration.GetValue("Guest:TokenMinutes", 60));
        var guestId = $"guest-{Guid.NewGuid():N}";
        _session.Store(new GuestSession { Id = guestId, ExpiresAt = expiresAt });
        await _session.SaveChangesAsync(cancellationToken);

        var token = GuestTokens.Create(_configuration["Guest:SigningKey"]!, guestId, expiresAt.UtcDateTime);
        return Ok(new GuestResponse(token, $"Guest-{RandomNumberGenerator.GetInt32(1000, 10000)}", expiresAt));
    }
}
