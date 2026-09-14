using JasperFx.Events.Aggregation;
using JasperFx.Events.Tags;
using Splendor.Application.Events;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.DecisionStates;

[BoundaryAggregate]
public class SplendorGameState
{
    public GameStatus Status { get; private set; }
    public string? CurrentPlayerId { get; private set; }
    public string? PendingGemReturnPlayerId { get; private set; }
    public string? PendingNobleSelectionPlayerId { get; private set; }
    public HashSet<string> EligibleNobleIds { get; } = new();
    public GemCollection MarketGems { get; private set; } = GemCollection.Empty;
    public List<string> PlayerOrder { get; } = new();
    public List<string> Market1 { get; } = new();
    public List<string> Market2 { get; } = new();
    public List<string> Market3 { get; } = new();
    public List<string> Deck1 { get; } = new();
    public List<string> Deck2 { get; } = new();
    public List<string> Deck3 { get; } = new();
    public List<string> Nobles { get; } = new();
    public Dictionary<string, PlayerState> Players { get; } = new();

    public static EventTagQuery Query(Guid gameId) =>
        new EventTagQuery()
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

    public void Apply(GameCreated _)
    {
        Status = GameStatus.Created;
    }

    public void Apply(GameStarted e)
    {
        Status = GameStatus.Started;
        MarketGems = e.MarketGems;
        Deck1.AddRange(e.Deck1);
        Deck2.AddRange(e.Deck2);
        Deck3.AddRange(e.Deck3);
        Market1.AddRange(e.Market1);
        Market2.AddRange(e.Market2);
        Market3.AddRange(e.Market3);
        Nobles.AddRange(e.Nobles);
    }

    public void Apply(GameFinished _)
    {
        Status = GameStatus.Finished;
        PendingNobleSelectionPlayerId = null;
        EligibleNobleIds.Clear();
    }

    public void Apply(GameDeleted _)
    {
        Status = GameStatus.Deleted;
        PendingNobleSelectionPlayerId = null;
        EligibleNobleIds.Clear();
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

    public void Apply(GemLimitResolved e)
    {
        PendingGemReturnPlayerId = null;
        MarketGems += e.ReturnedGems;

        if (Players.TryGetValue(e.PlayerId, out var player))
        {
            player.Apply(e);
        }
    }

    public void Apply(GemsOverflowDetected e)
    {
        PendingGemReturnPlayerId = e.PlayerId;
    }

    public void Apply(CardReserved e)
    {
        if (Players.TryGetValue(e.PlayerId, out var player))
        {
            player.Apply(e);
        }

        var card = Splendor.Domain.CardDefinitions.GetById(e.CardId);
        if (card != null)
        {
            MarketFor(card).Remove(e.CardId);
        }
    }

    public void Apply(NobleAcquired e)
    {
        Nobles.Remove(e.NobleId);
        PendingNobleSelectionPlayerId = null;
        EligibleNobleIds.Clear();

        if (Players.TryGetValue(e.PlayerId, out var player))
        {
            player.Apply(e);
        }
    }

    public void Apply(NobleSelectionRequired e)
    {
        PendingNobleSelectionPlayerId = e.PlayerId;
        EligibleNobleIds.Clear();
        foreach (var nobleId in e.EligibleNobleIds)
        {
            EligibleNobleIds.Add(nobleId);
        }
    }

    public void Apply(CardPurchased e)
    {
        if (Players.TryGetValue(e.PlayerId, out var player))
        {
            player.Apply(e);
        }

        MarketGems += e.PaidGems;

        var card = Splendor.Domain.CardDefinitions.GetById(e.CardId);
        if (card != null)
        {
            MarketFor(card).Remove(e.CardId);
        }
    }

    public void Apply(CardRevealed e)
    {
        MarketFor(e.Level).Add(e.CardId);
        DeckFor(e.Level).Remove(e.CardId);
    }

    internal string NextPlayerAfter(string playerId)
    {
        var index = PlayerOrder.IndexOf(playerId);
        if (index < 0) throw new InvalidOperationException("Player not found.");

        return PlayerOrder[(index + 1) % PlayerOrder.Count];
    }

    internal List<string> MarketFor(Splendor.Domain.ValueObjects.Card card) => MarketFor(card.Level);

    internal List<string> MarketFor(int level) =>
        level switch
        {
            1 => Market1,
            2 => Market2,
            3 => Market3,
            _ => throw new InvalidOperationException("Unknown card level.")
        };

    internal List<string> DeckFor(int level) =>
        level switch
        {
            1 => Deck1,
            2 => Deck2,
            3 => Deck3,
            _ => throw new InvalidOperationException("Unknown card level.")
        };

    public void Apply(IEnumerable<Domain.Common.IDomainEvent> events)
    {
        foreach (var e in events)
        {
            Apply(e);
        }
    }

    public void Apply(Domain.Common.IDomainEvent @event)
    {
        switch (@event)
        {
            case GameCreated e: Apply(e); break;
            case GameStarted e: Apply(e); break;
            case PlayerJoined e: Apply(e); break;
            case TurnStarted e: Apply(e); break;
            case GemsTaken e: Apply(e); break;
            case GemLimitResolved e: Apply(e); break;
            case GemsOverflowDetected e: Apply(e); break;
            case CardPurchased e: Apply(e); break;
            case CardRevealed e: Apply(e); break;
            case CardReserved e: Apply(e); break;
            case NobleSelectionRequired e: Apply(e); break;
            case NobleAcquired e: Apply(e); break;
            case GameFinished e: Apply(e); break;
            case GameDeleted e: Apply(e); break;
        }
    }
}
