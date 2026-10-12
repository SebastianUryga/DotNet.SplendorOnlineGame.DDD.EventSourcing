using System.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Splendor.Api.Auth;
using Splendor.Application.Commands;

namespace Splendor.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public AuthController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
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
        var guest = await _mediator.Send(new CreateGuestSessionCommand(
            _configuration.GetValue("Guest:MaxActive", 10),
            TimeSpan.FromMinutes(_configuration.GetValue("Guest:TokenMinutes", 60))), cancellationToken);

        if (guest is null)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Guest limit reached. Try again later." });
        }

        var token = GuestTokens.Create(_configuration["Guest:SigningKey"]!, guest.Id, guest.ExpiresAt.UtcDateTime);
        return Ok(new GuestResponse(token, $"Guest-{RandomNumberGenerator.GetInt32(1000, 10000)}", guest.ExpiresAt));
    }
}
