using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Splendor.Application.Commands;
using Splendor.Application.DecisionStates;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;
using Xunit;

namespace Splendor.UnitTests;

public class GameEndTests
{
    [Fact]
    public void TurnCompletion_DoesNotFinishGameUntilEveryPlayerCompletesTheRound()
    {
        var (gameId, _, _, player1Id, player2Id, history) = TestHelpers.CreateStartedGame(nobles: new List<string>());
        AddPurchasedCards(history, gameId, player1Id, "L3_02", "L3_03", "L3_06", "L2_02", "L1_07");

        var game = new SplendorGameState();
        TestHelpers.ApplyHistory(game, history);

        var produced = TurnCompletion.Decide(gameId, player1Id, game, DateTimeOffset.UtcNow).ToList();

        produced.OfType<GameFinished>().Should().BeEmpty();
        produced.OfType<TurnEnded>().Should().ContainSingle(e => e.PlayerId == player1Id);
        produced.OfType<TurnStarted>().Should().ContainSingle(e => e.PlayerId == player2Id);
    }

    [Fact]
    public void TurnCompletion_UsesFewerPurchasedCardsAsTieBreaker_WhenRoundCompletes()
    {
        var (gameId, _, _, player1Id, player2Id, history) = TestHelpers.CreateStartedGame(nobles: new List<string>());
        AddPurchasedCards(history, gameId, player1Id, "L3_02", "L3_03", "L3_06", "L3_01");
        AddPurchasedCards(history, gameId, player2Id, "L3_07", "L3_10", "L3_05", "L2_02", "L2_03");
        history.Add(new TurnStarted(gameId, player2Id, DateTimeOffset.UtcNow));

        var game = new SplendorGameState();
        TestHelpers.ApplyHistory(game, history);

        var produced = TurnCompletion.Decide(gameId, player2Id, game, DateTimeOffset.UtcNow).ToList();

        var finished = produced.OfType<GameFinished>().Should().ContainSingle().Subject;
        finished.WinnerId.Should().Be(player1Id);
        finished.PrestigePoints.Should().Be(15);
    }

    private static void AddPurchasedCards(List<object> history, Guid gameId, string playerId, params string[] cardIds)
    {
        foreach (var cardId in cardIds)
        {
            history.Add(new CardPurchased(gameId, playerId, cardId, GemCollection.Empty, DateTimeOffset.UtcNow));
        }
    }
}
