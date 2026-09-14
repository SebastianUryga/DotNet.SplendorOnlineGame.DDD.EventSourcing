using JasperFx.Events;
using Marten;
using Splendor.Application.DecisionStates;
using Splendor.Application.Events;
using Splendor.Domain;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.IntegrationTests;

public class GameTestHelper
{
    private readonly IDocumentStore _store;

    public GameTestHelper(IDocumentStore store)
    {
        _store = store;
    }

    public async Task<Guid> SeedStartedGameAsync()
    {
        var gameId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var level1 = CardDefinitions.GetLevel(1).Select(card => card.Id).ToList();
        var level2 = CardDefinitions.GetLevel(2).Select(card => card.Id).ToList();
        var level3 = CardDefinitions.GetLevel(3).Select(card => card.Id).ToList();

        await AppendAsync(gameId,
            new GameCreated(gameId, "owner-1", now),
            new PlayerJoined(gameId, "player-1", "owner-1", "Player 1", now),
            new PlayerJoined(gameId, "player-2", "owner-2", "Player 2", now),
            new GameStarted(
                gameId,
                new GemCollection(4, 4, 4, 4, 4, 5),
                level1.Skip(4).ToList(),
                level2.Skip(4).ToList(),
                level3.Skip(4).ToList(),
                level1.Take(4).ToList(),
                level2.Take(4).ToList(),
                level3.Take(4).ToList(),
                NobleDefinitions.AllNobles.Select(noble => noble.Id).ToList(),
                now),
            new TurnStarted(gameId, "player-1", now));

        return gameId;
    }

    public async Task AppendAsync(Guid gameId, params IDomainEvent[] events)
    {
        await using var session = _store.LightweightSession();
        session.Events.Append(
            Guid.NewGuid(),
            events.Select(session.TagEvent).ToArray());
        await session.SaveChangesAsync();
    }

    public async Task ExecuteAsync(Func<IDocumentSession, Task> action)
    {
        await using var session = _store.LightweightSession();
        await action(session);
    }

    public async Task<SplendorGameState?> LoadStateAsync(Guid gameId)
    {
        await using var session = _store.LightweightSession();
        return await session.Events.AggregateByTagsAsync<SplendorGameState>(
            SplendorGameState.Query(gameId));
    }
}
