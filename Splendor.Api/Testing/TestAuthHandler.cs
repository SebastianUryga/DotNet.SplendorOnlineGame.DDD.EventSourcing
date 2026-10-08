using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Splendor.Api.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Splendor.Api.Testing;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers.TryGetValue("X-Test-User-Id", out var headerValue)
            ? headerValue.ToString()
            : "ui-test-user";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "UI Test User"),
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        if (Request.Headers.TryGetValue("X-Test-Roles", out var roles))
            claims.AddRange(roles.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => new Claim(RoleClaims.Type, r)));
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
