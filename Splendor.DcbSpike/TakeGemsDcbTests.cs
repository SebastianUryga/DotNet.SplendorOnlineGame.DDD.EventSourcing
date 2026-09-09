using FluentAssertions;
using Marten;
using Splendor.DcbSpike.ValueObjects;
using Testcontainers.PostgreSql;

namespace Splendor.DcbSpike;

public class TakeGemsDcbTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private IDocumentStore _store = null!;

    [Fact]
    public async Task Takes_three_different_gems_in_own_turn()
    {
        var gameId = Guid.NewGuid();
        await SeedStartedGame(gameId);

        await using var session = _store.LightweightSession();
        var handler = new TakeGemsCommandHandler(session);

        await handler.Handle(new TakeGemsCommand
        {
            GameId = gameId,
            OwnerId = "owner-1",
            PlayerId = "player-1",
            Diamond = 1,
            Sapphire = 1,
            Emerald = 1
        }, CancellationToken.None);

        var state = await LoadState(gameId, "player-1");
        state!.CurrentPlayerId.Should().Be("player-2");
        state.MarketGems.Should().Be(new GemCollection(3, 3, 3, 4, 4, 5));
        state.Players["player-1"].Gems.Should().Be(new GemCollection(1, 1, 1, 0, 0, 0));
    }

    [Fact]
    public async Task Rejects_player_outside_current_turn()
    {
        var gameId = Guid.NewGuid();
        await SeedStartedGame(gameId);

        await using var session = _store.LightweightSession();
        var handler = new TakeGemsCommandHandler(session);

        var act = () => handler.Handle(new TakeGemsCommand
        {
            GameId = gameId,
            OwnerId = "owner-2",
            PlayerId = "player-2",
            Diamond = 1
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Not your turn.");
    }

    [Fact]
    public async Task Rejects_taking_two_same_gems_when_market_has_less_than_four()
    {
        var gameId = Guid.NewGuid();
        await SeedStartedGame(gameId);

        await using (var session = _store.LightweightSession())
        {
            session.Events.Append(Guid.NewGuid(),
                session.Tag(new GemsTaken(gameId, "player-2", new GemCollection(1, 0, 0, 0, 0, 0), DateTimeOffset.UtcNow)));
            await session.SaveChangesAsync(CancellationToken.None);
        }

        await using var commandSession = _store.LightweightSession();
        var handler = new TakeGemsCommandHandler(commandSession);

        var act = () => handler.Handle(new TakeGemsCommand
        {
            GameId = gameId,
            OwnerId = "owner-1",
            PlayerId = "player-1",
            Diamond = 2
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Not enough diamonds on market.");
    }

    [Fact]
    public async Task Buys_card_through_dcb_boundary()
    {
        var gameId = Guid.NewGuid();
        await SeedStartedGame(gameId, ["L1_03"], ["L1_05"]);

        await using (var seedSession = _store.LightweightSession())
        {
            seedSession.Events.Append(Guid.NewGuid(),
                seedSession.Tag(new GemsTaken(gameId, "player-1", new GemCollection(0, 3, 0, 0, 0, 0), DateTimeOffset.UtcNow)));
            await seedSession.SaveChangesAsync(CancellationToken.None);
        }

        await using var session = _store.LightweightSession();
        var handler = new BuyCardCommandHandler(session);

        await handler.Handle(
            new BuyCardCommand(gameId, "owner-1", "player-1", "L1_03"),
            CancellationToken.None);

        var state = await LoadBuyCardState(gameId);
        state!.CurrentPlayerId.Should().Be("player-2");
        state.Market1.Should().Contain("L1_05");
        state.Players["player-1"].OwnedCardIds.Should().Contain("L1_03");
    }

    [Fact]
    public async Task Projects_summary_and_splendor_board_read_models()
    {
        var gameId = Guid.NewGuid();
        await using var writeSession = _store.LightweightSession();
        var now = DateTimeOffset.UtcNow;

        writeSession.Events.Append(Guid.NewGuid(),
            writeSession.Tag(new GameCreated(gameId, "owner-1", now)),
            writeSession.Tag(new PlayerJoined(gameId, "player-1", "owner-1", "Player 1", now)),
            writeSession.Tag(new PlayerJoined(gameId, "player-2", "owner-2", "Player 2", now)),
            writeSession.Tag(new GameStarted(
                gameId,
                StartGameCommandHandler.StartingMarketGems(2),
                CardCatalog.GetCardIdsByLevel(1).Skip(4).ToList(),
                CardCatalog.GetCardIdsByLevel(2).Skip(4).ToList(),
                CardCatalog.GetCardIdsByLevel(3).Skip(4).ToList(),
                CardCatalog.GetCardIdsByLevel(1).Take(4).ToList(),
                CardCatalog.GetCardIdsByLevel(2).Take(4).ToList(),
                CardCatalog.GetCardIdsByLevel(3).Take(4).ToList(),
                [],
                now)),
            writeSession.Tag(new TurnStarted(gameId, "player-1", now)));
        await writeSession.SaveChangesAsync(CancellationToken.None);

        await using var querySession = _store.QuerySession();
        var summaries = await new GetGamesQueryHandler(querySession).Handle(includeDeleted: false, CancellationToken.None);
        var board = await new GetSplendorBoardQueryHandler(querySession).Handle(gameId, CancellationToken.None);

        summaries.Should().ContainSingle(x =>
            x.Id == gameId &&
            x.GameType == "Splendor" &&
            x.Status == "Started" &&
            x.PlayerCount == 2 &&
            x.CurrentPlayerId == "player-1");

        board.Should().NotBeNull();
        board!.Status.Should().Be("Started");
        board.MarketGems.Should().Be(StartGameCommandHandler.StartingMarketGems(2));
        board.Market1.Should().BeEquivalentTo(CardCatalog.GetCardIdsByLevel(1).Take(4));
        board.Players.Should().HaveCount(2);
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _store = MartenDcb.CreateStore(_postgres.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        _store.Dispose();
        await _postgres.DisposeAsync();
    }

    private async Task SeedStartedGame(Guid gameId, List<string>? market1 = null, List<string>? deck1 = null)
    {
        await using var session = _store.LightweightSession();
        var now = DateTimeOffset.UtcNow;

        session.Events.Append(Guid.NewGuid(),
            session.Tag(new PlayerJoined(gameId, "player-1", "owner-1", "Player 1", now)),
            session.Tag(new PlayerJoined(gameId, "player-2", "owner-2", "Player 2", now)),
            session.Tag(new GameStarted(gameId, StartGameCommandHandler.StartingMarketGems(2), deck1 ?? [], [], [], market1 ?? [], [], [], [], now)),
            session.Tag(new TurnStarted(gameId, "player-1", now)));

        await session.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<TakeGemsDecisionState?> LoadState(Guid gameId, string playerId)
    {
        await using var session = _store.LightweightSession();
        return await session.Events.AggregateByTagsAsync<TakeGemsDecisionState>(
            TakeGemsDecisionState.Query(gameId),
            CancellationToken.None);
    }

    private async Task<BuyCardDecisionState?> LoadBuyCardState(Guid gameId)
    {
        await using var session = _store.LightweightSession();
        return await session.Events.AggregateByTagsAsync<BuyCardDecisionState>(
            BuyCardDecisionState.Query(gameId),
            CancellationToken.None);
    }
}
