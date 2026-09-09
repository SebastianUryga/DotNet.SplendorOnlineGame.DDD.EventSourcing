using FluentAssertions;
using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike;

public class StartGameDecisionStateTests
{
    [Fact]
    public void Starts_three_player_game_with_official_token_count()
    {
        var gameId = Guid.NewGuid();
        var state = new StartGameDecisionState();
        var now = DateTimeOffset.UtcNow;

        state.Apply(new GameCreated(gameId, "owner-1", now));
        state.Apply(new PlayerJoined(gameId, "player-1", "owner-1", "Player 1", now));
        state.Apply(new PlayerJoined(gameId, "player-2", "owner-2", "Player 2", now));
        state.Apply(new PlayerJoined(gameId, "player-3", "owner-3", "Player 3", now));

        var events = StartGameCommandHandler.Decide(new StartGameCommand(gameId, "owner-1"), state);

        events.OfType<GameStarted>().Should().ContainSingle(e =>
            e.MarketGems == new GemCollection(5, 5, 5, 5, 5, 5));
        events.OfType<TurnStarted>().Should().ContainSingle(e => e.PlayerId == "player-1");
    }
}
