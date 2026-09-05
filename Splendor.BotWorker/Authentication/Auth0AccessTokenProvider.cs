using Microsoft.IdentityModel.JsonWebTokens;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Splendor.BotWorker.Authentication;

public class Auth0AccessTokenProvider : IAccessTokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private string? _accessToken;
    private string? _userId;
    private DateTimeOffset _expiresAt;

    public Auth0AccessTokenProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _expiresAt.AddMinutes(-1))
        {
            return _accessToken;
        }

        var domain = _configuration["Auth0:Domain"]!;
        var request = new TokenRequest(
            GrantType: "http://auth0.com/oauth/grant-type/password-realm",
            Username: _configuration["Bot:Username"]!,
            Password: _configuration["Bot:Password"]!,
            Audience: _configuration["Auth0:Audience"]!,
            ClientId: _configuration["Auth0:ClientId"]!,
            Scope: "openid profile email",
            Realm: _configuration["Auth0:Realm"] ?? "Username-Password-Authentication");

        var response = await _httpClient.PostAsJsonAsync($"https://{domain}/oauth/token", request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);

        _accessToken = token?.AccessToken
            ?? throw new InvalidOperationException("Auth0 did not return access_token.");

        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
        _userId = ReadUserIdFromJwt(_accessToken);

        return _accessToken;
    }

    public async Task<string> GetUserIdAsync(CancellationToken cancellationToken)
    {
        await GetAccessTokenAsync(cancellationToken);

        return _userId ?? throw new InvalidOperationException("Bot user id is not available.");
    }

    private static string ReadUserIdFromJwt(string accessToken)
    {
        var handler = new JsonWebTokenHandler();
        var jwt = handler.ReadJsonWebToken(accessToken);

        return jwt.GetClaim("sub")?.Value
            ?? jwt.GetClaim(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("Access token does not contain 'sub' claim.");
    }

    private record TokenRequest(
        [property: JsonPropertyName("grant_type")] string GrantType,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("password")] string Password,
        [property: JsonPropertyName("audience")] string Audience,
        [property: JsonPropertyName("client_id")] string ClientId,
        [property: JsonPropertyName("scope")] string Scope,
        [property: JsonPropertyName("realm")] string Realm);

    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("token_type")] string TokenType);
}