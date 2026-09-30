using FluentAssertions;
using Splendor.Application.Commands;
using Splendor.Application.Snapshots;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;
using Xunit;

namespace Splendor.UnitTests;

public class ChooseNobleTests
{
    [Fact]
    public void ChooseNoble_AcquiresOnlySelectedNoble_WhenTwoAreEligible()
    {
        var (gameId, ownerId, _, playerId, _, history) = TestHelpers.CreateStartedGame(["N_06", "N_07"]);
        TestHelpers.AddPurchasedCardsWithBonus(history, gameId, playerId, GemType.Diamond, 4);
        TestHelpers.AddPurchasedCardsWithBonus(history, gameId, playerId, GemType.Onyx, 4);
        TestHelpers.AddPurchasedCardsWithBonus(history, gameId, playerId, GemType.Sapphire, 4);
        history.Add(new NobleSelectionRequired(gameId, playerId, ["N_06", "N_07"], DateTimeOffset.UtcNow));

        var state = new SplendorGameState();
        TestHelpers.ApplyHistory(state, history);

        var events = ChooseNobleCommandHandler.Decide(new ChooseNobleCommand
        {
            GameId = gameId,
            OwnerId = ownerId,
            PlayerId = playerId,
            NobleId = "N_06"
        }, state).ToList();
        state.Apply(events);
        events.AddRange(TurnCompletion.DecideAfterNobleSelection(gameId, playerId, state, DateTimeOffset.UtcNow));

        events.OfType<NobleAcquired>().Should().ContainSingle()
            .Which.NobleId.Should().Be("N_06");
    }
}
