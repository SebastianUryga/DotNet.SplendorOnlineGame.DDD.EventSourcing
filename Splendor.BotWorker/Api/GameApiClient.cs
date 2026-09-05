using Splendor.BotWorker.Authentication;
using Splendor.Contracts.Games;
using Splendor.Domain.ValueObjects;
using System.Net.Http.Json;

namespace Splendor.BotWorker.Api;

public sealed class GameApiClient : IGameApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAccessTokenProvider _tokenProvider;

    public GameApiClient(HttpClient httpClient, IAccessTokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
    }

    public async Task<GameView?> GetGameAsync(Guid gameId, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"/games/{gameId}", cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GameView>(cancellationToken);
    }


    public Task TakeGemsAsync(Guid gameId, TakeGemsRequest request, CancellationToken cancellationToken)
    {

        return PostAsync($"games/{gameId}/actions/take-gems", request, cancellationToken);
    }

    public Task BuyCardAsync(Guid gameId, BuyCardRequest request, CancellationToken cancellationToken)
    {
        return PostAsync($"games/{gameId}/actions/buy-card", request, cancellationToken);
    }

    public Task ReserveCardAsync(Guid gameId, ReserveCardRequest request, CancellationToken cancellationToken)
    {
        return PostAsync($"games/{gameId}/actions/reserve-card", request, cancellationToken);
    }

    public Task ResolveGemLimitAsync(Guid gameId, ResolveGemLimitRequest request, CancellationToken cancellationToken)
    {
        return PostAsync($"games/{gameId}/actions/resolve-gem-limit", request, cancellationToken);
    }

    public Task ChooseNobleAsync(Guid gameId, ChooseNobleRequest request, CancellationToken cancellationToken)
    {
        return PostAsync($"games/{gameId}/actions/choose-noble", request, cancellationToken);
    }

    public async Task<IReadOnlyList<Card>> GetCardsAsync(CancellationToken cancellationToken)
    {
        var cards = await _httpClient.GetFromJsonAsync<IReadOnlyList<Card>>("/cards",cancellationToken: cancellationToken);

        return cards ?? throw new InvalidOperationException("API returned empty cards response.");
    }

    private async Task PostAsync<T>(string url, T body, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, url, cancellationToken);

        request.Content = JsonContent.Create(body);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);

        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return request;
    }
}