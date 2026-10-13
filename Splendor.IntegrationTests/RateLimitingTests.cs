using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Splendor.IntegrationTests;

public class RateLimitingTests : IClassFixture<SplendorApiFactory>
{
    private const int GameActionsPermitLimit = 10;
    private const int GlobalPermitLimit = 60;
    private readonly HttpClient _client;

    public RateLimitingTests(SplendorApiFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GameActions_AllowsPermitLimitThenReturnsTooManyRequests()
    {
        var userId = $"rl-{Guid.NewGuid()}";

        for (var i = 0; i < GameActionsPermitLimit; i++)
        {
            var response = await PostGameActionAsUserAsync(userId);
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        var limited = await PostGameActionAsUserAsync(userId);
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GameActions_LimitIsPerUser()
    {
        var exhaustedUserId = $"rl-{Guid.NewGuid()}";
        for (var i = 0; i <= GameActionsPermitLimit; i++)
        {
            await PostGameActionAsUserAsync(exhaustedUserId);
        }

        var otherUserResponse = await PostGameActionAsUserAsync($"rl-{Guid.NewGuid()}");

        otherUserResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GlobalLimiter_LimitsEndpointWithoutRateLimitingAttribute()
    {
        var userId = $"rl-{Guid.NewGuid()}";

        for (var i = 0; i < GlobalPermitLimit; i++)
        {
            var response = await GetCardsAsUserAsync(userId);
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        var limited = await GetCardsAsUserAsync(userId);
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private async Task<HttpResponseMessage> GetCardsAsUserAsync(string userId)
    {
        using (TestUserContext.SetUser(userId))
        {
            return await _client.GetAsync("/cards");
        }
    }

    // The limiter runs before the controller, so the (missing) game only needs to reach the endpoint.
    private async Task<HttpResponseMessage> PostGameActionAsUserAsync(string userId)
    {
        using (TestUserContext.SetUser(userId))
        {
            return await _client.PostAsJsonAsync($"/games/{Guid.NewGuid()}/start", new { });
        }
    }
}
