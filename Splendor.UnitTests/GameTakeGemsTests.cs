using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Splendor.Domain.Aggregates;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;
using Xunit;

namespace Splendor.UnitTests;

public class GameTakeGemsTests
{
    [Fact]
    public void TakeGems_EmitsOverflowDetected_WhenTotalExceedsLimit()
    {
        // arrange - build a started game with two players
        var (gameId, owner1, owner2, player1Id, player2Id, history) = TestHelpers.CreateStartedGame();

        // give the player 9 gems (setup via events)
        history.Add(new GemsTaken(gameId, player1Id, new GemCollection(1,1,1,1,1,4), DateTimeOffset.UtcNow));

        var game = new Game();
        TestHelpers.ApplyHistory(game, history);

        // act - player attempts to take 2 more gems -> total becomes 11 -> overflow
        var take = new GemCollection(1, 1, 0, 0, 0, 0);
        var produced = game.TakeGems(owner1, player1Id, take).ToList();

        // assert - expect GemsTaken then GemsOverflowDetected, with excess = 1
        produced.Should().HaveCount(2);
        produced[0].Should().BeOfType<GemsTaken>();
        var overflow = produced[1].Should().BeOfType<GemsOverflowDetected>().Subject;
        overflow.ExcessCount.Should().Be(1);
        overflow.PlayerId.Should().Be(player1Id);
    }

    [Fact]
    public void TakeGems_RequestsNobleChoice_WhenMoreThanOneNobleIsEligible()
    {
        var (gameId, owner1, _, player1Id, _, history) = TestHelpers.CreateStartedGame();

        TestHelpers.AddPurchasedCardsWithBonus(history, gameId, player1Id, GemType.Diamond, 4);
        TestHelpers.AddPurchasedCardsWithBonus(history, gameId, player1Id, GemType.Onyx, 4);
        TestHelpers.AddPurchasedCardsWithBonus(history, gameId, player1Id, GemType.Sapphire, 4);

        var game = new Game();
        TestHelpers.ApplyHistory(game, history);

        // no matter what action the player takes, they will now be eligible for two nobles (N_06 and N_07)
        var produced = game.TakeGems(owner1, player1Id, new GemCollection(1, 1, 1, 0, 0, 0)).ToList();

        var selection = produced.OfType<NobleSelectionRequired>().Single();
        selection.EligibleNobleIds.Should().BeEquivalentTo("N_06", "N_07");
        produced.OfType<TurnEnded>().Should().BeEmpty();
        produced.OfType<NobleAcquired>().Should().BeEmpty();
    }
}
