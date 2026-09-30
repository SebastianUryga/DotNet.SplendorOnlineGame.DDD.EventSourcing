using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Splendor.Application.Commands;
using Splendor.Application.DecisionStates;
using Splendor.Application.Snapshots;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;
using Xunit;

namespace Splendor.UnitTests;

public class GameReserveCardTests
{
    [Fact]
    public void ReserveCard_ReservesTopCardFromSelectedDeck_WhenCardIdIsNull()
    {
        var (gameId, owner1, _, player1Id, _, history) = TestHelpers.CreateStartedGame();
        var game = new SplendorGameState();
        TestHelpers.ApplyHistory(game, history);
        var topCardId = game.Deck2.First();

        var command = new ReserveCardCommand
        {
            GameId = gameId,
            OwnerId = owner1,
            PlayerId = player1Id,
            Level = 2
        };

        var produced = ReserveCardCommandHandler.Decide(
            command,
            game,
            TestHelpers.CreateActiveTurnClock(gameId, player1Id, DateTimeOffset.UtcNow));

        var reserved = produced.Should().ContainSingle(e => e is CardReserved)
            .Which.Should().BeOfType<CardReserved>().Subject;
        reserved.CardId.Should().Be(topCardId);
        produced.Should().NotContain(e => e is CardRevealed);

        game.Apply(produced);
        game.Deck2.Should().NotContain(topCardId);
        game.Players[player1Id].ReservedCardIds.Should().Contain(topCardId);
    }

    [Fact]
    public void ReserveCard_GivesGoldAndEmitsOverflow_WhenTotalExceedsLimit()
    {
        // arrange - build a started game with two players
        var (gameId, owner1, owner2, player1Id, player2Id, history) = TestHelpers.CreateStartedGame();

        // give the player 10 gems (setup via events)
        history.Add(new GemsTaken(gameId, player1Id, new GemCollection(2,2,2,2,2,0), DateTimeOffset.UtcNow));

        var game = new SplendorGameState();
        TestHelpers.ApplyHistory(game, history);

        // pick a card available in market
        var cardId = game.Market1.First();

        // act - player attempts to reserve a market card; market has gold so player will be given one -> overflow
        var command = new ReserveCardCommand
        {
            GameId = gameId,
            OwnerId = owner1,
            PlayerId = player1Id,
            CardId = cardId
        };
        var now = DateTimeOffset.UtcNow;
        var produced = ReserveCardCommandHandler.Decide(
            command, game, TestHelpers.CreateActiveTurnClock(gameId, player1Id, now)).ToList();
        game.Apply(produced);
        produced.AddRange(TurnCompletion.DecideAfterAction(gameId, player1Id, game, DateTimeOffset.UtcNow));

        // assert - expect GemsTaken (gold), CardReserved, CardRevealed, then GemsOverflowDetected with excess = 1
        produced.Should().HaveCountGreaterOrEqualTo(4);
        produced[0].Should().BeOfType<GemsTaken>();
        produced[1].Should().BeOfType<CardReserved>();
        // CardRevealed should be present (replacement from deck)
        produced[2].Should().BeOfType<CardRevealed>();
        var overflow = produced.Last().Should().BeOfType<GemsOverflowDetected>().Subject;
        overflow.ExcessCount.Should().Be(1);
        overflow.PlayerId.Should().Be(player1Id);
    }

    [Fact]
    public void ReserveCard_Throws_WhenPlayerAlreadyHasThreeReservedCards()
    {
        var (gameId, owner1, _, player1Id, _, history) = TestHelpers.CreateStartedGame();
        history.Add(new CardReserved(gameId, player1Id, "L1_01", DateTimeOffset.UtcNow));
        history.Add(new CardReserved(gameId, player1Id, "L1_02", DateTimeOffset.UtcNow));
        history.Add(new CardReserved(gameId, player1Id, "L1_03", DateTimeOffset.UtcNow));

        var game = new SplendorGameState();
        TestHelpers.ApplyHistory(game, history);

        var command = new ReserveCardCommand
        {
            GameId = gameId,
            OwnerId = owner1,
            PlayerId = player1Id,
            CardId = game.Market1.First()
        };

        var now = DateTimeOffset.UtcNow;
        var clock = TestHelpers.CreateActiveTurnClock(gameId, player1Id, now);
        var act = () => ReserveCardCommandHandler.Decide(command, game, clock);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot reserve more than 3 cards.");
    }
}
