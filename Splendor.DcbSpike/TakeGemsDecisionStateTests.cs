using FluentAssertions;
using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike;

public class TakeGemsDecisionStateTests
{
    [Fact]
    public void Emits_turn_events_after_valid_take()
    {
        var gameId = Guid.NewGuid();
        var state = StartedGame(gameId);

        var events = TakeGemsCommandHandler.Decide(new TakeGemsCommand
        {
            GameId = gameId,
            OwnerId = "owner-1",
            PlayerId = "player-1",
            Diamond = 1,
            Sapphire = 1,
            Emerald = 1
        }, state);

        events.OfType<GemsTaken>().Should().ContainSingle();
        events.OfType<TurnEnded>().Should().ContainSingle();
        events.OfType<TurnStarted>().Should().ContainSingle(e => e.PlayerId == "player-2");
    }

    [Fact]
    public void Rejects_take_outside_current_turn()
    {
        var gameId = Guid.NewGuid();
        var state = StartedGame(gameId);

        var act = () => TakeGemsCommandHandler.Decide(new TakeGemsCommand
        {
            GameId = gameId,
            OwnerId = "owner-2",
            PlayerId = "player-2",
            Diamond = 1
        }, state);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Not your turn.");
    }

    private static TakeGemsDecisionState StartedGame(Guid gameId)
    {
        var state = new TakeGemsDecisionState();
        var now = DateTimeOffset.UtcNow;

        state.Apply(new PlayerJoined(gameId, "player-1", "owner-1", "Player 1", now));
        state.Apply(new PlayerJoined(gameId, "player-2", "owner-2", "Player 2", now));
        state.Apply(new GameStarted(gameId, StartGameCommandHandler.StartingMarketGems(2), [], [], [], [], [], [], [], now));
        state.Apply(new TurnStarted(gameId, "player-1", now));

        return state;
    }
}
