using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Splendor.Domain.Aggregates;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;
using Xunit;

namespace Splendor.UnitTests;

public class GameReserveCardTests
{
    [Fact]
    public void ReserveCard_GivesGoldAndEmitsOverflow_WhenTotalExceedsLimit()
    {
        // arrange - build a started game with two players
        var (gameId, owner1, owner2, player1Id, player2Id, history) = TestHelpers.CreateStartedGame();

        // give the player 10 gems (setup via events)
        history.Add(new GemsTaken(gameId, player1Id, new GemCollection(2,2,2,2,2,0), DateTimeOffset.UtcNow));

        var game = new Game();
        TestHelpers.ApplyHistory(game, history);

        // pick a card available in market
        var cardId = game.Market1.First();

        // act - player attempts to reserve a market card; market has gold so player will be given one -> overflow
        var produced = game.ReserveCard(owner1, player1Id, cardId).ToList();

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
}
