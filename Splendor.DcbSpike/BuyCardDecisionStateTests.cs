using FluentAssertions;
using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike;

public class BuyCardDecisionStateTests
{
    [Fact]
    public void Buys_available_market_card_and_reveals_replacement()
    {
        var gameId = Guid.NewGuid();
        var state = StartedGame(gameId);
        state.Apply(new GemsTaken(gameId, "player-1", new GemCollection(0, 3, 0, 0, 0, 0), DateTimeOffset.UtcNow));

        var events = BuyCardCommandHandler.Decide(new BuyCardCommand(gameId, "owner-1", "player-1", "L1_03"), state);

        events.OfType<CardPurchased>().Should().ContainSingle(e =>
            e.CardId == "L1_03" &&
            e.PaidGems == new GemCollection(0, 3, 0, 0, 0, 0));
        events.OfType<CardRevealed>().Should().ContainSingle(e => e.CardId == "L1_05");
        events.OfType<TurnStarted>().Should().ContainSingle(e => e.PlayerId == "player-2");
    }

    [Fact]
    public void Rejects_card_player_cannot_afford()
    {
        var gameId = Guid.NewGuid();
        var state = StartedGame(gameId);

        var act = () => BuyCardCommandHandler.Decide(new BuyCardCommand(gameId, "owner-1", "player-1", "L1_04"), state);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot afford this card.");
    }

    [Fact]
    public void Buys_reserved_card_without_revealing_replacement()
    {
        var gameId = Guid.NewGuid();
        var state = StartedGame(gameId);
        var now = DateTimeOffset.UtcNow;

        state.Apply(new CardReserved(gameId, "player-1", "L1_03", now));
        state.Apply(new GemsTaken(gameId, "player-1", new GemCollection(0, 3, 0, 0, 0, 0), now));

        var events = BuyCardCommandHandler.Decide(new BuyCardCommand(gameId, "owner-1", "player-1", "L1_03"), state);

        events.OfType<CardPurchased>().Should().ContainSingle();
        events.OfType<CardRevealed>().Should().BeEmpty();
    }

    private static BuyCardDecisionState StartedGame(Guid gameId)
    {
        var state = new BuyCardDecisionState();
        var now = DateTimeOffset.UtcNow;

        state.Apply(new PlayerJoined(gameId, "player-1", "owner-1", "Player 1", now));
        state.Apply(new PlayerJoined(gameId, "player-2", "owner-2", "Player 2", now));
        state.Apply(new GameStarted(
            gameId,
            StartGameCommandHandler.StartingMarketGems(2),
            ["L1_05"],
            [],
            [],
            ["L1_01", "L1_02", "L1_03", "L1_04"],
            [],
            [],
            [],
            now));
        state.Apply(new TurnStarted(gameId, "player-1", now));

        return state;
    }
}
