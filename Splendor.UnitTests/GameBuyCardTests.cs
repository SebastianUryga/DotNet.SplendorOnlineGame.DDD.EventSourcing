using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Splendor.Domain;
using Splendor.Domain.Aggregates;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;
using Xunit;

namespace Splendor.UnitTests;

public class GameBuyCardTests
{
    [Fact]
    public void BuyCard_EmitsCardPurchased_WhenPlayerCanAfford()
    {
        // arrange - create started game
        var (gameId, owner1, owner2, player1Id, player2Id, history) = TestHelpers.CreateStartedGame();

        // pick a card from the initial markets (from the GameStarted event in history)
        var started = history.OfType<GameStarted>().First();
        var cardId = started.Market1.FirstOrDefault() ?? started.Market2.FirstOrDefault() ?? started.Market3.First();
        var card = CardDefinitions.GetById(cardId) ?? throw new InvalidOperationException("Card not found in definitions");

        // give the player exactly the cost of the card
        history.Add(new GemsTaken(gameId, player1Id, card.Cost, DateTimeOffset.UtcNow));

        var game = new Game();
        TestHelpers.ApplyHistory(game, history);

        // act
        var produced = game.BuyCard(owner1, player1Id, cardId).ToList();

        // assert - expect CardPurchased first
        produced.Should().ContainSingle(e => e is CardPurchased);
        var purchased = produced.OfType<CardPurchased>().First();
        purchased.PlayerId.Should().Be(player1Id);
        purchased.CardId.Should().Be(cardId);
        purchased.PaidGems.Should().Be(card.Cost);
    }
}
