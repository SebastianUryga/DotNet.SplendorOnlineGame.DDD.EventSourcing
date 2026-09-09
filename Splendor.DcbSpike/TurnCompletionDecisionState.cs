using JasperFx.Events.Aggregation;
using JasperFx.Events.Tags;
using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike;

[BoundaryAggregate]
internal class TurnCompletionDecisionState
{
    public GameStatus Status { get; private set; }
    public string? CurrentPlayerId { get; private set; }
    public List<string> PlayerOrder { get; } = new();
    public List<string> Nobles { get; } = new();
    public Dictionary<string, PlayerState> Players { get; } = new();

    public static EventTagQuery Query(Guid gameId) =>
        new EventTagQuery()
            .Or<GameCreated, GameTag>(new GameTag(gameId))
            .Or<GameStarted, GameTag>(new GameTag(gameId))
            .Or<PlayerJoined, GameTag>(new GameTag(gameId))
            .Or<TurnStarted, GameTag>(new GameTag(gameId))
            .Or<GemsTaken, GameTag>(new GameTag(gameId))
            .Or<GemLimitResolved, GameTag>(new GameTag(gameId))
            .Or<CardPurchased, GameTag>(new GameTag(gameId))
            .Or<NobleAcquired, GameTag>(new GameTag(gameId))
            .Or<GameFinished, GameTag>(new GameTag(gameId))
            .Or<GameDeleted, GameTag>(new GameTag(gameId));

    public static TurnCompletionDecisionState From(ITurnCompletionSource source)
    {
        var state = new TurnCompletionDecisionState
        {
            Status = source.Status,
            CurrentPlayerId = source.CurrentPlayerId
        };

        state.PlayerOrder.AddRange(source.PlayerOrder);
        state.Nobles.AddRange(source.Nobles);

        foreach (var (playerId, player) in source.Players)
        {
            state.Players[playerId] = player.Clone();
        }

        return state;
    }

    public void Apply(IEnumerable<IDomainEvent> events)
    {
        foreach (var @event in events)
        {
            Apply(@event);
        }
    }

    public void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case GameCreated e: Apply(e); break;
            case GameStarted e: Apply(e); break;
            case PlayerJoined e: Apply(e); break;
            case TurnStarted e: Apply(e); break;
            case GemsTaken e: Apply(e); break;
            case GemLimitResolved e: Apply(e); break;
            case CardPurchased e: Apply(e); break;
            case NobleAcquired e: Apply(e); break;
            case GameFinished e: Apply(e); break;
            case GameDeleted e: Apply(e); break;
        }
    }

    public void Apply(GameCreated _)
    {
        Status = GameStatus.Created;
    }

    public void Apply(GameStarted e)
    {
        Status = GameStatus.Started;
        Nobles.AddRange(e.Nobles);
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
        if (Players.TryGetValue(e.PlayerId, out var player))
        {
            player.Apply(e);
        }
    }

    public void Apply(GemLimitResolved e)
    {
        if (Players.TryGetValue(e.PlayerId, out var player))
        {
            player.Apply(e);
        }
    }

    public void Apply(CardPurchased e)
    {
        if (Players.TryGetValue(e.PlayerId, out var player))
        {
            player.Apply(e);
        }
    }

    public void Apply(NobleAcquired e)
    {
        Nobles.Remove(e.NobleId);
    }

    public void Apply(GameFinished _)
    {
        Status = GameStatus.Finished;
    }

    public void Apply(GameDeleted _)
    {
        Status = GameStatus.Deleted;
    }

    internal string NextPlayerAfter(string playerId)
    {
        var index = PlayerOrder.IndexOf(playerId);
        if (index < 0) throw new InvalidOperationException("Player not found.");

        return PlayerOrder[(index + 1) % PlayerOrder.Count];
    }
}
