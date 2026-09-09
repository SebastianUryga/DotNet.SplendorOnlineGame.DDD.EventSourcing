using FluentAssertions;
using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike;

public class TurnCompletionTests
{
    [Fact]
    public void Requires_noble_selection_when_player_is_eligible_for_multiple_nobles()
    {
        var gameId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var state = new TurnCompletionDecisionState();

        state.Apply(new PlayerJoined(gameId, "player-1", "owner-1", "Player 1", now));
        state.Apply(new PlayerJoined(gameId, "player-2", "owner-2", "Player 2", now));
        state.Apply(new GameStarted(gameId, GemCollection.Empty, [], [], [], [], [], [], ["N_01", "N_02"], now));
        state.Apply(new TurnStarted(gameId, "player-1", now));

        foreach (var cardId in new[]
        {
            "L1_01", "L1_02", "L1_03",
            "L1_09", "L1_10", "L1_11",
            "L1_17", "L1_18", "L1_19",
            "L1_25", "L1_26", "L1_27"
        })
        {
            state.Apply(new CardPurchased(gameId, "player-1", cardId, GemCollection.Empty, now));
        }

        var events = TurnCompletion.Decide(gameId, "player-1", state, now);

        events.OfType<NobleSelectionRequired>().Should().ContainSingle(e =>
            e.EligibleNobleIds.Contains("N_01") &&
            e.EligibleNobleIds.Contains("N_02"));
        events.OfType<TurnEnded>().Should().BeEmpty();
        events.OfType<TurnStarted>().Should().BeEmpty();
    }
}
