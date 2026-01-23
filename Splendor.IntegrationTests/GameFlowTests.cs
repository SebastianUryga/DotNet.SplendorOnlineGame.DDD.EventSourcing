using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Splendor.Application.ReadModels;
using Splendor.Domain.ValueObjects;
using Xunit;

namespace Splendor.IntegrationTests;

public class GameFlowTests : IClassFixture<SplendorApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private List<Card>? _allCards;

    public GameFlowTests(SplendorApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullGameFlow_TwoPlayers_CanPlayUntilCardPurchase()
    {
        // Arrange - two different users
        const string user1Id = "user-alice";
        const string user2Id = "user-bob";

        // 1. User1 creates a game
        Guid gameId;
        using (TestCurrentUserService.SetUser(user1Id))
        {
            var response = await _client.PostAsJsonAsync("/games", new { });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            gameId = json.GetProperty("id").GetGuid();
        }

        // 2. User1 joins as "Alice"
        using (TestCurrentUserService.SetUser(user1Id))
        {
            var response = await _client.PostAsJsonAsync($"/games/{gameId}/players", new { Name = "Alice" });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // 3. User2 joins as "Bob"
        using (TestCurrentUserService.SetUser(user2Id))
        {
            var response = await _client.PostAsJsonAsync($"/games/{gameId}/players", new { Name = "Bob" });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // 4. User1 starts the game
        using (TestCurrentUserService.SetUser(user1Id))
        {
            var response = await _client.PostAsJsonAsync($"/games/{gameId}/start", new { });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // 5. Verify game state after start
        var game = await GetGame(gameId);
        game.Status.Should().Be("Started");
        game.Players.Should().HaveCount(2);
        game.CurrentPlayerId.Should().NotBeNullOrEmpty();
        game.Market1.Should().HaveCount(4);
        game.Market2.Should().HaveCount(4);
        game.Market3.Should().HaveCount(4);
        game.MarketGems.Diamond.Should().BeGreaterThan(0);
        game.MarketGems.Gold.Should().BeGreaterThan(0);


        // 6. Play several rounds - each player takes gems to accumulate enough to buy something
        for (int round = 0; round < 6; round++)
        {
            game = await GetGame(gameId);
            var currentPlayer = game.Players.First(p => p.Id == game.CurrentPlayerId);
            var userId = currentPlayer.OwnerId;

            // Take 3 different gems (rotate which ones)
            var gemsToTake = (round % 3) switch
            {
                0 => new { PlayerId = currentPlayer.Id, Diamond = 1, Sapphire = 1, Emerald = 1, Ruby = 0, Onyx = 0, Gold = 0 },
                1 => new { PlayerId = currentPlayer.Id, Diamond = 0, Sapphire = 0, Emerald = 1, Ruby = 1, Onyx = 1, Gold = 0 },
                _ => new { PlayerId = currentPlayer.Id, Diamond = 1, Sapphire = 1, Emerald = 0, Ruby = 0, Onyx = 1, Gold = 0 }
            };

            using (TestCurrentUserService.SetUser(userId))
            {
                var response = await _client.PostAsJsonAsync($"/games/{gameId}/actions/take-gems", gemsToTake);
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        // 7. Verify players have accumulated gems
        game = await GetGame(gameId);
        var totalPlayerGems = game.Players.Sum(p =>
            p.Gems.Diamond + p.Gems.Sapphire + p.Gems.Emerald + p.Gems.Ruby + p.Gems.Onyx);
        totalPlayerGems.Should().Be(18); // 6 rounds * 3 gems

        // 8. Find a card in Market1 that the current player can afford
        game = await GetGame(gameId);
        var buyer = game.Players.First(p => p.Id == game.CurrentPlayerId);
        
        var affordableCard = (await Task.WhenAll(game.Market1.Select(id => GetCardDefinition(id))))
            .FirstOrDefault(card => CanAfford(buyer.Gems, card.Cost));


        affordableCard.Should().NotBeNull("With 9 gems each, players should be able to afford at least one Level 1 card");

        // 9. Buy the card
        using (TestCurrentUserService.SetUser(buyer.OwnerId))
        {
            var response = await _client.PostAsJsonAsync($"/games/{gameId}/actions/buy-card",
                new { PlayerId = buyer.Id, CardId = affordableCard!.Id });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // 10. Verify card was purchased
        game = await GetGame(gameId);
        var updatedBuyer = game.Players.First(p => p.Id == buyer.Id);
        updatedBuyer.OwnedCardIds.Should().Contain(affordableCard!.Id);
        game.Market1.Should().NotContain(affordableCard!.Id);
    }

    [Fact]
    public async Task TakeGems_WrongPlayer_ReturnsBadRequest()
    {
        // Arrange
        var gameId = await CreateAndStartGame();
        var game = await GetGame(gameId);
        var notCurrentPlayer = game.Players.First(p => p.Id != game.CurrentPlayerId);

        // Act - try to take gems as wrong player
        using (TestCurrentUserService.SetUser(notCurrentPlayer.OwnerId))
        {
            var response = await _client.PostAsJsonAsync($"/games/{gameId}/actions/take-gems",
                new { PlayerId = notCurrentPlayer.Id, Diamond = 1, Sapphire = 1, Emerald = 1, Ruby = 0, Onyx = 0, Gold = 0 });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    private async Task<Guid> CreateAndStartGame()
    {
        const string user1 = "user-1";
        const string user2 = "user-2";

        Guid gameId;
        using (TestCurrentUserService.SetUser(user1))
        {
            var response = await _client.PostAsJsonAsync("/games", new { });
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            gameId = json.GetProperty("id").GetGuid();
        }

        using (TestCurrentUserService.SetUser(user1))
            await _client.PostAsJsonAsync($"/games/{gameId}/players", new { Name = "P1" });

        using (TestCurrentUserService.SetUser(user2))
            await _client.PostAsJsonAsync($"/games/{gameId}/players", new { Name = "P2" });

        using (TestCurrentUserService.SetUser(user1))
            await _client.PostAsJsonAsync($"/games/{gameId}/start", new { });

        return gameId;
    }

    private async Task<GameView> GetGame(Guid gameId)
    {
        var response = await _client.GetAsync($"/games/{gameId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GameView>(JsonOptions))!;
    }

    private bool CanAfford(GemCollection playerGems, GemCollection cost)
    {
        return playerGems.Diamond >= cost.Diamond &&
               playerGems.Sapphire >= cost.Sapphire &&
               playerGems.Emerald >= cost.Emerald &&
               playerGems.Ruby >= cost.Ruby &&
               playerGems.Onyx >= cost.Onyx;
    }

    private async Task<Card> GetCardDefinition(string cardId)
    {
        if (_allCards == null)
        {
            _allCards = await _client.GetFromJsonAsync<List<Card>>("/cards", JsonOptions);
        }
        return _allCards!.First(c => c.Id == cardId);
    }
}
