using System;
using System.Collections.Generic;
using Splendor.Domain;
using Splendor.Domain.Aggregates;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.UnitTests;

public static class TestHelpers
{
    // Apply a list of domain events to an aggregate using dynamic dispatch
    public static void ApplyHistory(Game game, IEnumerable<object> history)
    {
        foreach (var e in history)
        {
            game.Apply((dynamic)e);
        }
    }

    // Create a started game with two players and populated decks/markets. Returns tuple with ids and history events.
    public static (Guid GameId, string Owner1, string Owner2, string Player1Id, string Player2Id, List<object> History) CreateStartedGame()
    {
        var gameId = Guid.NewGuid();
        var owner1 = "owner-1";
        var owner2 = "owner-2";
        var player1Id = Guid.NewGuid().ToString() + " Alice";
        var player2Id = Guid.NewGuid().ToString() + " Bob";

        // Build decks and markets similar to Game.StartGame behaviour
        var random = new Random();
        var deck1 = CardDefinitions.GetLevel(1).Select(c => c.Id).OrderBy(_ => random.Next()).ToList();
        var deck2 = CardDefinitions.GetLevel(2).Select(c => c.Id).OrderBy(_ => random.Next()).ToList();
        var deck3 = CardDefinitions.GetLevel(3).Select(c => c.Id).OrderBy(_ => random.Next()).ToList();

        var market1 = deck1.Take(4).ToList(); deck1 = deck1.Skip(4).ToList();
        var market2 = deck2.Take(4).ToList(); deck2 = deck2.Skip(4).ToList();
        var market3 = deck3.Take(4).ToList(); deck3 = deck3.Skip(4).ToList();

        var history = new List<object>
        {
            new GameCreated(gameId, owner1, DateTimeOffset.UtcNow),
            new PlayerJoined(gameId, player1Id, owner1, "Alice", DateTimeOffset.UtcNow),
            new PlayerJoined(gameId, player2Id, owner2, "Bob", DateTimeOffset.UtcNow),
            new GameStarted(gameId, deck1, deck2, deck3, market1, market2, market3, DateTimeOffset.UtcNow),
            new TurnStarted(gameId, player1Id, DateTimeOffset.UtcNow)
        };

        return (gameId, owner1, owner2, player1Id, player2Id, history);
    }
}
