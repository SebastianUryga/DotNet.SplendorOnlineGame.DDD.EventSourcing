using System.Text.Json.Serialization;
using Splendor.Application.DecisionStates;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Snapshots;

public class SplendorGameState
{
    public Guid Id { get; set; }
    [JsonPropertyName("CreatorId")]
    public string GameCreatorId { get; set; } = string.Empty;
    public GameStatus Status { get; set; }
    public string? CurrentPlayerId { get; set; }
    public string? PendingGemReturnPlayerId { get; set; }
    public string? PendingNobleSelectionPlayerId { get; set; }
    public HashSet<string> EligibleNobleIds { get; set; } = new();
    public GemCollection MarketGems { get; set; } = GemCollection.Empty;
    public List<string> PlayerOrder { get; set; } = new();
    public List<string> Market1 { get; set; } = new();
    public List<string> Market2 { get; set; } = new();
    public List<string> Market3 { get; set; } = new();
    public List<string> Deck1 { get; set; } = new();
    public List<string> Deck2 { get; set; } = new();
    public List<string> Deck3 { get; set; } = new();
    public List<string> Nobles { get; set; } = new();
    public Dictionary<string, PlayerState> Players { get; set; } = new();

    //public static EventTagQuery Query(Guid gameId) =>
    //    new EventTagQuery()
    //        .Or<GameStarted, GameTag>(new GameTag(gameId))
    //....

    public void Apply(GameCreated e)
    {
        Id = e.GameId;
        GameCreatorId = e.CreatorId;
        Status = GameStatus.Created;
    }

    public void Apply(GameStarted e)
    {
        Status = GameStatus.Started;
        MarketGems = e.MarketGems ?? GemCollection.Empty;
        Deck1 = e.Deck1 ?? [];
        Deck2 = e.Deck2 ?? [];
        Deck3 = e.Deck3 ?? [];
        Market1 = e.Market1 ?? [];
        Market2 = e.Market2 ?? [];
        Market3 = e.Market3 ?? [];
        Nobles = e.Nobles ?? [];
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

        Players[e.PlayerId] = new PlayerState(e.PlayerOwnerId, e.Name);
    }

    public void Apply(PlayerLeft e)
    {
        PlayerOrder.Remove(e.PlayerId);
        Players.Remove(e.PlayerId);
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
            DeckFor(card.Level).Remove(e.CardId);
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

        var card = Domain.CardDefinitions.GetById(e.CardId);
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

    internal List<string> MarketFor(Card card) => MarketFor(card.Level);

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
            case PlayerLeft e: Apply(e); break;
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
