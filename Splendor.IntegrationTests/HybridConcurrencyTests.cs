using FluentAssertions;
using JasperFx.Events;
using Marten;
using Marten.Events;
using Microsoft.Extensions.DependencyInjection;
using Splendor.Application.Commands;
using Splendor.Application.DecisionStates;
using Splendor.Application.Events;
using Splendor.Application.Snapshots;
using Splendor.Domain;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.IntegrationTests;

public class HybridConcurrencyTests : IClassFixture<SplendorApiFactory>
{
    private readonly IDocumentStore _store;

    public HybridConcurrencyTests(SplendorApiFactory factory)
    {
        _store = factory.Services.GetRequiredService<IDocumentStore>();
    }

    [Fact]
    public async Task JoinGame_RejectsThirdActiveGameAndAllowsItAfterOneIsDeleted()
    {
        const string ownerId = "limited-owner";
        var firstGameId = await SeedCreatedGame();
        var secondGameId = await SeedCreatedGame();
        var thirdGameId = await SeedCreatedGame();

        await Join(firstGameId, ownerId, "Alice");
        await Join(secondGameId, ownerId, "Bob");

        var exception = await Record.ExceptionAsync(() => Join(thirdGameId, ownerId, "Carol"));

        exception.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("You cannot be active in more than 2 games.");

        await Execute(session => new DeleteGameCommandHandler(session).Handle(
            new DeleteGameCommand(firstGameId), CancellationToken.None));
        await Join(thirdGameId, ownerId, "Carol");

        await using var session = _store.LightweightSession();
        var state = (await session.Events.FetchForWriting<SplendorGameState>(thirdGameId)).Aggregate;
        state!.Players.Values.Should().ContainSingle(player => player.OwnerId == ownerId);
    }

    [Fact]
    public async Task MigratedHandlers_UseGameSnapshotAndStream()
    {
        const string owner1 = "migrated-owner-1";
        const string owner2 = "migrated-owner-2";
        var gameId = await SeedCreatedGame(owner1);

        await Execute(session => new JoinGameCommandHandler(session).Handle(new JoinGameCommand
        {
            GameId = gameId,
            OwnerId = owner1,
            Name = "Alice"
        }, CancellationToken.None));
        await Execute(session => new JoinGameCommandHandler(session).Handle(new JoinGameCommand
        {
            GameId = gameId,
            OwnerId = owner2,
            Name = "Bob"
        }, CancellationToken.None));
        await Execute(session => new InvitePlayerCommandHandler(session).Handle(new InvitePlayerCommand
        {
            GameId = gameId,
            OwnerId = owner1,
            InviteeId = "owner-3"
        }, CancellationToken.None));
        await Execute(session => new StartGameCommandHandler(session, TimeProvider.System).Handle(
            new StartGameCommand(gameId, owner1), CancellationToken.None));
        await Execute(session => new DeleteGameCommandHandler(session).Handle(
            new DeleteGameCommand(gameId), CancellationToken.None));

        await using var freshSession = _store.LightweightSession();
        var state = (await freshSession.Events.FetchForWriting<SplendorGameState>(gameId)).Aggregate;
        var stream = await freshSession.Events.FetchStreamAsync(gameId);

        state.Should().NotBeNull();
        state!.CreatorId.Should().Be(owner1);
        state.Status.Should().Be(GameStatus.Deleted);
        state.Players.Should().HaveCount(2);
        stream.Should().Contain(e => e.Data is PlayerInvited);
        stream.Should().OnlyContain(e => e.StreamId == gameId);
    }

    [Fact]
    public async Task FetchForWriting_RejectsStaleStreamVersion()
    {
        var gameId = await SeedCreatedGame();
        await using var first = _store.LightweightSession();
        await using var second = _store.LightweightSession();
        var firstStream = await first.Events.FetchForWriting<SplendorGameState>(gameId);
        var secondStream = await second.Events.FetchForWriting<SplendorGameState>(gameId);

        firstStream.AppendOne(first.TagEvent(Joined(gameId, "owner-1", "Alice")));
        secondStream.AppendOne(second.TagEvent(Joined(gameId, "owner-2", "Bob")));

        await first.SaveChangesAsync();
        var exception = await Record.ExceptionAsync(() => second.SaveChangesAsync());

        exception.Should().BeOfType<EventStreamUnexpectedMaxEventIdException>();
    }

    [Fact]
    public async Task FetchForWritingByTags_RejectsStaleOwnerBoundaryAcrossStreams()
    {
        var firstGameId = await SeedCreatedGame();
        var secondGameId = await SeedCreatedGame();
        const string ownerId = "shared-owner";
        await using var first = _store.LightweightSession();
        await using var second = _store.LightweightSession();
        var firstStream = await first.Events.FetchForWriting<SplendorGameState>(firstGameId);
        var secondStream = await second.Events.FetchForWriting<SplendorGameState>(secondGameId);
        await first.Events.FetchForWritingByTags<JoinGameDecisionState>(
            JoinGameDecisionState.Query(ownerId));
        await second.Events.FetchForWritingByTags<JoinGameDecisionState>(
            JoinGameDecisionState.Query(ownerId));

        firstStream.AppendOne(first.TagEvent(Joined(firstGameId, ownerId, "Alice")));
        secondStream.AppendOne(second.TagEvent(Joined(secondGameId, ownerId, "Bob")));

        await first.SaveChangesAsync();
        var exception = await Record.ExceptionAsync(() => second.SaveChangesAsync());

        exception.Should().BeOfType<DcbConcurrencyException>();
    }

    [Fact]
    public async Task HybridAppend_WritesPlayerJoinedToGameStream()
    {
        var gameId = await SeedCreatedGame();
        await using (var session = _store.LightweightSession())
        {
            var stream = await session.Events.FetchForWriting<SplendorGameState>(gameId);
            await session.Events.FetchForWritingByTags<JoinGameDecisionState>(
                JoinGameDecisionState.Query("owner-1"));

            stream.AppendOne(session.TagEvent(Joined(gameId, "owner-1", "Alice")));
            await session.SaveChangesAsync();
        }

        await using var query = _store.QuerySession();
        var joined = (await query.Events.FetchStreamAsync(gameId))
            .Single(e => e.Data is PlayerJoined);

        joined.StreamId.Should().Be(gameId);
    }

    [Fact]
    public async Task FetchForWriting_InNewSessionSeesJoinedPlayer()
    {
        var gameId = await SeedCreatedGame();
        await using (var session = _store.LightweightSession())
        {
            var stream = await session.Events.FetchForWriting<SplendorGameState>(gameId);
            await session.Events.FetchForWritingByTags<JoinGameDecisionState>(
                JoinGameDecisionState.Query("owner-1"));
            stream.AppendOne(session.TagEvent(Joined(gameId, "owner-1", "Alice")));
            await session.SaveChangesAsync();
        }

        await using var freshSession = _store.LightweightSession();
        var state = (await freshSession.Events.FetchForWriting<SplendorGameState>(gameId)).Aggregate;

        state.Should().NotBeNull();
        state!.PlayerOrder.Should().ContainSingle();
        state.Players.Values.Should().ContainSingle(player => player.Name == "Alice");
    }

    [Fact]
    public async Task AsyncSnapshot_RoundTripsGetOnlyCollections()
    {
        var gameId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var level1 = CardDefinitions.GetLevel(1).Select(card => card.Id).ToList();
        var level2 = CardDefinitions.GetLevel(2).Select(card => card.Id).ToList();
        var level3 = CardDefinitions.GetLevel(3).Select(card => card.Id).ToList();
        var nobles = NobleDefinitions.AllNobles.Select(noble => noble.Id).ToList();
        await using (var session = _store.LightweightSession())
        {
            session.Events.StartStream(gameId,
                session.TagEvent(new GameCreated(gameId, "owner-1", now)),
                session.TagEvent(new PlayerJoined(gameId, "player-1", "owner-1", "Alice", now)),
                session.TagEvent(new GameStarted(
                    gameId,
                    new GemCollection(4, 4, 4, 4, 4, 5),
                    level1.Skip(4).ToList(), level2.Skip(4).ToList(), level3.Skip(4).ToList(),
                    level1.Take(4).ToList(), level2.Take(4).ToList(), level3.Take(4).ToList(),
                    nobles,
                    now)));
            await session.SaveChangesAsync();
        }

        await _store.WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(10));
        await using var freshSession = _store.QuerySession();
        var snapshot = await freshSession.LoadAsync<SplendorGameState>(gameId);

        snapshot.Should().NotBeNull();
        snapshot!.CreatorId.Should().Be("owner-1");
        snapshot.PlayerOrder.Should().Equal("player-1");
        snapshot.Players.Should().ContainKey("player-1");
        snapshot.Deck1.Should().Equal(level1.Skip(4));
        snapshot.Deck2.Should().Equal(level2.Skip(4));
        snapshot.Deck3.Should().Equal(level3.Skip(4));
        snapshot.Market1.Should().Equal(level1.Take(4));
        snapshot.Market2.Should().Equal(level2.Take(4));
        snapshot.Market3.Should().Equal(level3.Take(4));
        snapshot.Nobles.Should().Equal(nobles);
    }

    private async Task<Guid> SeedCreatedGame(string? creatorId = null)
    {
        var gameId = Guid.NewGuid();
        await using var session = _store.LightweightSession();
        session.Events.StartStream(gameId,
            session.TagEvent(new GameCreated(gameId, creatorId ?? $"creator-{gameId}", DateTimeOffset.UtcNow)));
        await session.SaveChangesAsync();
        return gameId;
    }

    private async Task Execute(Func<IDocumentSession, Task> action)
    {
        await using var session = _store.LightweightSession();
        await action(session);
    }

    private Task Join(Guid gameId, string ownerId, string name) =>
        Execute(session => new JoinGameCommandHandler(session).Handle(new JoinGameCommand
        {
            GameId = gameId,
            OwnerId = ownerId,
            Name = name
        }, CancellationToken.None));

    private static PlayerJoined Joined(Guid gameId, string ownerId, string name) =>
        new(gameId, $"player-{Guid.NewGuid()}", ownerId, name, DateTimeOffset.UtcNow);
}
