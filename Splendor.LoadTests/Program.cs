using JasperFx.Events;
using JasperFx.Events.Projections;
using JasperFx.Events.Tags;
using Marten;
using Npgsql;
using Splendor.Application.Events;
using Splendor.Application.Snapshots;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;
using System.Diagnostics;
using System.Globalization;
using Testcontainers.PostgreSql;

const int sampleCount = 30;
int[] eventCounts = [100, 1_000, 10_000];

Debug.Assert(Percentile([1, 2, 3, 4], 0.50) == 2);

await using var postgres = new PostgreSqlBuilder("postgres:16").Build();
Console.WriteLine("Starting isolated PostgreSQL...");
await postgres.StartAsync();

var connectionString = postgres.GetConnectionString();
var results = new List<Result>();

using (var store = CreateStore(connectionString, snapshots: false))
{
    foreach (var eventCount in eventCounts)
    {
        results.Add(await RunScenario(store, "dcb", "tiny", eventCount, FetchMode.Dcb));
        results.Add(await RunScenario(store, "dcb", "realistic", eventCount, FetchMode.Dcb));
        results.Add(await RunScenario(store, "stream-live", "realistic", eventCount, FetchMode.Stream));
    }
}

using (var store = CreateStore(connectionString, snapshots: true))
{
    foreach (var eventCount in eventCounts)
    {
        results.Add(await RunScenario(store, "stream-snapshot-inline", "realistic", eventCount, FetchMode.Stream, verifySnapshot: true));
    }
}

var databaseBytes = await GetDatabaseSize(connectionString);
var outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"event-store-load-test-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
await File.WriteAllLinesAsync(outputPath,
[
    "strategy,event_profile,events,samples,fetch_p50_ms,fetch_p95_ms,fetch_p99_ms,total_p50_ms,total_p95_ms,total_p99_ms,database_bytes_total",
    .. results.Select(result => string.Join(',',
        result.Strategy,
        result.EventProfile,
        result.Events,
        sampleCount,
        Format(result.FetchP50),
        Format(result.FetchP95),
        Format(result.FetchP99),
        Format(result.TotalP50),
        Format(result.TotalP95),
        Format(result.TotalP99),
        databaseBytes))
]);

Console.WriteLine($"Database: {databaseBytes / 1024d / 1024d:F2} MiB");
Console.WriteLine($"Results: {outputPath}");

static IDocumentStore CreateStore(string connectionString, bool snapshots)
{
    return DocumentStore.For(options =>
    {
        options.Connection(connectionString);
        options.AutoCreateSchemaObjects = JasperFx.AutoCreate.All;
        options.Events.StreamIdentity = StreamIdentity.AsGuid;
        options.Events.RegisterTagType<GameTag>("game");

        if (snapshots)
        {
            options.Projections.Snapshot<BenchmarkGameState>(SnapshotLifecycle.Inline);
        }
    });
}

static async Task<Result> RunScenario(
    IDocumentStore store,
    string strategy,
    string eventProfile,
    int eventCount,
    FetchMode mode,
    bool verifySnapshot = false)
{
    var gameId = await SeedGame(store, eventCount, eventProfile);

    if (verifySnapshot)
    {
        await using var session = store.QuerySession();
        _ = await session.LoadAsync<BenchmarkGameState>(gameId)
            ?? throw new InvalidOperationException("Snapshot was not created.");
    }

    await Fetch(store, gameId, mode);

    var fetchTimes = new List<double>(sampleCount);
    var totalTimes = new List<double>(sampleCount);
    for (var sample = 0; sample < sampleCount; sample++)
    {
        var (fetch, total) = await ExecuteCommand(store, gameId, mode);
        fetchTimes.Add(fetch.TotalMilliseconds);
        totalTimes.Add(total.TotalMilliseconds);
    }

    var result = new Result(
        strategy,
        eventProfile,
        eventCount,
        Percentile(fetchTimes, 0.50),
        Percentile(fetchTimes, 0.95),
        Percentile(fetchTimes, 0.99),
        Percentile(totalTimes, 0.50),
        Percentile(totalTimes, 0.95),
        Percentile(totalTimes, 0.99));

    Console.WriteLine(
        $"{strategy,-22} | {eventProfile,-9} | {eventCount,6:N0} events | " +
        $"fetch p50/p95/p99: {result.FetchP50:F2}/{result.FetchP95:F2}/{result.FetchP99:F2} ms | " +
        $"total: {result.TotalP50:F2}/{result.TotalP95:F2}/{result.TotalP99:F2} ms");

    return result;
}

static async Task<Guid> SeedGame(IDocumentStore store, int eventCount, string eventProfile)
{
    var gameId = Guid.NewGuid();
    var now = DateTimeOffset.UtcNow;
    var oneDiamond = new GemCollection(1, 0, 0, 0, 0, 0);
    var events = new List<IDomainEvent>(eventCount)
    {
        new GameCreated(gameId, "owner-1", now),
        new PlayerJoined(gameId, "player-1", "owner-1", "Player 1", now),
        new GameStarted(gameId, new GemCollection(4, 4, 4, 4, 4, 5), [], [], [], [], [], [], [], now),
        new TurnStarted(gameId, "player-1", now)
    };

    while (events.Count < eventCount)
    {
        if (eventProfile == "tiny")
        {
            events.Add(new TurnStarted(gameId, "player-1", now));
            continue;
        }

        // ponytail: bounded synthetic cycle; replace with recorded legal games if event-shape fidelity matters.
        events.Add(((events.Count - 4) % 3) switch
        {
            0 => (IDomainEvent)new GemsTaken(gameId, "player-1", oneDiamond, now),
            1 => new GemLimitResolved(gameId, "player-1", oneDiamond, now),
            _ => new TurnStarted(gameId, "player-1", now)
        });
    }

    await using var session = store.LightweightSession();
    session.Events.StartStream(gameId, events.Select(session.TagEvent).ToArray());
    await session.SaveChangesAsync();
    return gameId;
}

static async Task Fetch(IDocumentStore store, Guid gameId, FetchMode mode)
{
    await using var session = store.LightweightSession();
    if (mode == FetchMode.Dcb)
    {
        var eventTagQuery = new EventTagQuery()
            .Or<GameStarted, GameTag>(new GameTag(gameId))
            .Or<GameCreated, GameTag>(new GameTag(gameId))
            .Or<PlayerJoined, GameTag>(new GameTag(gameId))
            .Or<TurnStarted, GameTag>(new GameTag(gameId))
            .Or<GemsTaken, GameTag>(new GameTag(gameId))
            .Or<GemLimitResolved, GameTag>(new GameTag(gameId))
            .Or<GemsOverflowDetected, GameTag>(new GameTag(gameId))
            .Or<CardPurchased, GameTag>(new GameTag(gameId))
            .Or<CardRevealed, GameTag>(new GameTag(gameId))
            .Or<CardReserved, GameTag>(new GameTag(gameId))
            .Or<NobleSelectionRequired, GameTag>(new GameTag(gameId))
            .Or<NobleAcquired, GameTag>(new GameTag(gameId))
            .Or<GameFinished, GameTag>(new GameTag(gameId))
            .Or<GameDeleted, GameTag>(new GameTag(gameId));
        var boundary = await session.Events.FetchForWritingByTags<BenchmarkGameState>(eventTagQuery);
        _ = boundary.Aggregate ?? throw new InvalidOperationException("Seeded game not found.");
    }
    else
    {
        var stream = await session.Events.FetchForWriting<BenchmarkGameState>(gameId);
        _ = stream.Aggregate ?? throw new InvalidOperationException("Seeded game not found.");
    }
}

static async Task<(TimeSpan Fetch, TimeSpan Total)> ExecuteCommand(IDocumentStore store, Guid gameId, FetchMode mode)
{
    var total = Stopwatch.StartNew();
    await using var session = store.LightweightSession();
    var nextEvent = new TurnStarted(gameId, "player-1", DateTimeOffset.UtcNow);

    var fetch = Stopwatch.StartNew();
    if (mode == FetchMode.Dcb)
    {
        var eventTagQuery = new EventTagQuery()
            .Or<GameStarted, GameTag>(new GameTag(gameId))
            .Or<GameCreated, GameTag>(new GameTag(gameId))
            .Or<PlayerJoined, GameTag>(new GameTag(gameId))
            .Or<TurnStarted, GameTag>(new GameTag(gameId))
            .Or<GemsTaken, GameTag>(new GameTag(gameId))
            .Or<GemLimitResolved, GameTag>(new GameTag(gameId))
            .Or<GemsOverflowDetected, GameTag>(new GameTag(gameId))
            .Or<CardPurchased, GameTag>(new GameTag(gameId))
            .Or<CardRevealed, GameTag>(new GameTag(gameId))
            .Or<CardReserved, GameTag>(new GameTag(gameId))
            .Or<NobleSelectionRequired, GameTag>(new GameTag(gameId))
            .Or<NobleAcquired, GameTag>(new GameTag(gameId))
            .Or<GameFinished, GameTag>(new GameTag(gameId))
            .Or<GameDeleted, GameTag>(new GameTag(gameId));
        var boundary = await session.Events.FetchForWritingByTags<BenchmarkGameState>(eventTagQuery);
        fetch.Stop();
        _ = boundary.Aggregate ?? throw new InvalidOperationException("Seeded game not found.");
        boundary.AppendMany(session.TagEvent(nextEvent));
    }
    else
    {
        var stream = await session.Events.FetchForWriting<BenchmarkGameState>(gameId);
        fetch.Stop();
        _ = stream.Aggregate ?? throw new InvalidOperationException("Seeded game not found.");
        stream.AppendOne(session.TagEvent(nextEvent));
    }

    await session.SaveChangesAsync();
    total.Stop();
    return (fetch.Elapsed, total.Elapsed);
}

static async Task<long> GetDatabaseSize(string connectionString)
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new NpgsqlCommand("select pg_database_size(current_database())", connection);
    return (long)(await command.ExecuteScalarAsync() ?? 0L);
}

static double Percentile(IEnumerable<double> values, double percentile)
{
    var sorted = values.Order().ToArray();
    return sorted[(int)Math.Ceiling(percentile * sorted.Length) - 1];
}

static string Format(double value) => value.ToString("F3", CultureInfo.InvariantCulture);

enum FetchMode
{
    Dcb,
    Stream
}

record Result(
    string Strategy,
    string EventProfile,
    int Events,
    double FetchP50,
    double FetchP95,
    double FetchP99,
    double TotalP50,
    double TotalP95,
    double TotalP99);

public class BenchmarkGameState
{
    public Guid Id { get; set; }
    public string? CurrentPlayerId { get; set; }
    public GemCollection MarketGems { get; set; } = GemCollection.Empty;
    public GemCollection PlayerGems { get; set; } = GemCollection.Empty;

    public void Apply(GameCreated e) => Id = e.GameId;
    public void Apply(GameStarted e) => MarketGems = e.MarketGems;
    public void Apply(TurnStarted e) => CurrentPlayerId = e.PlayerId;

    public void Apply(GemsTaken e)
    {
        MarketGems -= e.Gems;
        PlayerGems += e.Gems;
    }

    public void Apply(GemLimitResolved e)
    {
        MarketGems += e.ReturnedGems;
        PlayerGems -= e.ReturnedGems;
    }
}
