using JasperFx.Events.Aggregation;
using JasperFx.Events.Tags;
using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike;

[BoundaryAggregate]
public class TakeGemsDecisionState : ITurnCompletionSource
{
    public GameStatus Status { get; private set; }
    public bool Started => Status == GameStatus.Started;
    public string? CurrentPlayerId { get; private set; }
    public string? PendingGemReturnPlayerId { get; private set; }
    public GemCollection MarketGems { get; private set; } = GemCollection.Empty;
    public List<string> PlayerOrder { get; } = new();
    public Dictionary<string, PlayerState> Players { get; } = new();
    public List<string> Nobles { get; } = new();

    public static EventTagQuery Query(Guid gameId) =>
        new EventTagQuery()
            .Or<GameStarted, GameTag>(new GameTag(gameId))
            .Or<GameCreated, GameTag>(new GameTag(gameId))
            .Or<PlayerJoined, GameTag>(new GameTag(gameId))
            .Or<TurnStarted, GameTag>(new GameTag(gameId))
            .Or<GemsTaken, GameTag>(new GameTag(gameId))
            .Or<GemLimitResolved, GameTag>(new GameTag(gameId))
            .Or<GemsOverflowDetected, GameTag>(new GameTag(gameId))
            .Or<NobleAcquired, GameTag>(new GameTag(gameId))
            .Or<GameFinished, GameTag>(new GameTag(gameId))
            .Or<GameDeleted, GameTag>(new GameTag(gameId));

    public void Apply(GameCreated _)
    {
        Status = GameStatus.Created;
    }

    public void Apply(GameStarted _)
    {
        Status = GameStatus.Started;
        MarketGems = _.MarketGems;
    }

    public void Apply(GameFinished _)
    {
        Status = GameStatus.Finished;
    }

    public void Apply(GameDeleted _)
    {
        Status = GameStatus.Deleted;
    }

    public void Apply(PlayerJoined e)
    {
        if (!Players.ContainsKey(e.PlayerId))
        {
            PlayerOrder.Add(e.PlayerId);
        }

        Players[e.PlayerId] = new PlayerState(e.OwnerId, e.Name);
    }

    public void Apply(TurnStarted e)
    {
        CurrentPlayerId = e.PlayerId;
    }

    public void Apply(GemsTaken e)
    {
        MarketGems -= e.Gems;

        if (Players.TryGetValue(e.PlayerId, out var player))
        {
            player.Apply(e);
        }
    }

    public void Apply(GemsOverflowDetected e)
    {
        PendingGemReturnPlayerId = e.PlayerId;
    }

    public void Apply(NobleAcquired e)
    {
        Nobles.Remove(e.NobleId);
    }

    public void Apply(GemLimitResolved e)
    {
        PendingGemReturnPlayerId = null;
        MarketGems += e.ReturnedGems;

        if (Players.TryGetValue(e.PlayerId, out var player))
        {
            player.Apply(e);
        }
    }

    internal string NextPlayerAfter(string playerId)
    {
        var index = PlayerOrder.IndexOf(playerId);
        if (index < 0) throw new InvalidOperationException("Player not found.");

        return PlayerOrder[(index + 1) % PlayerOrder.Count];
    }
}
