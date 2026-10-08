using Marten.Events.Aggregation;
using Splendor.Application.ReadModels;
using Splendor.Domain;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.Infrastructure.Projections;

public partial class SplendorBoardProjection : SingleStreamProjection<SplendorBoardView, Guid>
{
    public SplendorBoardView Create(GameCreated e) => new()
    {
        Id = e.GameId,
        Status = GameStatus.Created,
        UpdatedAt = e.Timestamp,
        GameVersion = 1
    };

    public void Apply(PlayerJoined e, SplendorBoardView view)
    {
        view.Players.Add(new PlayerBoardView
        {
            Id = e.PlayerId,
            OwnerId = e.PlayerOwnerId,
            Name = e.Name
        });
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(PlayerLeft e, SplendorBoardView view)
    {
        view.Players.RemoveAll(player => player.Id == e.PlayerId);
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GameStarted e, SplendorBoardView view)
    {
        view.Status = GameStatus.Started;
        view.MarketGems = e.MarketGems ?? GemCollection.Empty;
        view.Market1 = e.Market1;
        view.Market2 = e.Market2;
        view.Market3 = e.Market3;
        view.Deck1Count = e.Deck1.Count;
        view.Deck2Count = e.Deck2.Count;
        view.Deck3Count = e.Deck3.Count;
        view.Nobles = e.Nobles ?? new List<string>();
        view.CurrentPlayerId = view.Players.FirstOrDefault()?.Id;
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(TurnStarted e, SplendorBoardView view)
    {
        view.CurrentPlayerId = e.PlayerId;
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(TurnDeadlineStarted e, SplendorBoardView view)
    {
        view.TurnId = e.TurnId;
        view.ExpiresAt = e.ExpiresAt;
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(TurnExpired e, SplendorBoardView view)
    {
        ClearTurnDeadline(view);
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GemsTaken e, SplendorBoardView view)
    {
        ClearTurnDeadline(view);
        view.MarketGems -= e.Gems;

        var player = view.Players.FirstOrDefault(p => p.Id == e.PlayerId);
        if (player != null)
        {
            player.Gems += e.Gems;
        }

        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GemsOverflowDetected e, SplendorBoardView view)
    {
        view.IsGemReturnPending = true;
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GemLimitResolved e, SplendorBoardView view)
    {
        view.MarketGems += e.ReturnedGems;
        view.IsGemReturnPending = false;

        var player = view.Players.FirstOrDefault(p => p.Id == e.PlayerId);
        if (player != null)
        {
            player.Gems -= e.ReturnedGems;
        }

        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(CardPurchased e, SplendorBoardView view)
    {
        ClearTurnDeadline(view);
        var card = CardDefinitions.GetById(e.CardId);
        var player = view.Players.FirstOrDefault(p => p.Id == e.PlayerId);
        if (player != null)
        {
            player.Gems -= e.PaidGems;
            player.OwnedCardIds.Add(e.CardId);
            player.ReservedCardIds.Remove(e.CardId);
            player.PrestigePoints += card?.PrestigePoints ?? 0;
        }

        view.MarketGems += e.PaidGems;
        if (card != null)
        {
            GetMarketForLevel(view, card.Level).Remove(e.CardId);
        }

        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(CardRevealed e, SplendorBoardView view)
    {
        GetMarketForLevel(view, e.Level).Add(e.CardId);
        DecrementDeckCount(view, e.Level);
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(CardReserved e, SplendorBoardView view)
    {
        ClearTurnDeadline(view);
        var card = CardDefinitions.GetById(e.CardId);
        if (card != null)
        {
            var reservedFromMarket = GetMarketForLevel(view, card.Level).Remove(e.CardId);
            if (!reservedFromMarket)
            {
                DecrementDeckCount(view, card.Level);
            }
        }

        var player = view.Players.FirstOrDefault(p => p.Id == e.PlayerId);
        if (player != null)
        {
            player.ReservedCardIds.Add(e.CardId);
        }

        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(NobleSelectionRequired e, SplendorBoardView view)
    {
        view.PlayerIdAwaitingNobleSelection = e.PlayerId;
        view.EligibleNobleIds = e.EligibleNobleIds.ToList();
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(NobleAcquired e, SplendorBoardView view)
    {
        var player = view.Players.FirstOrDefault(p => p.Id == e.PlayerId);
        if (player != null)
        {
            player.OwnedNobleIds.Add(e.NobleId);
            player.PrestigePoints += NobleDefinitions.GetById(e.NobleId)?.PrestigePoints ?? 0;
        }

        view.Nobles.Remove(e.NobleId);
        view.PlayerIdAwaitingNobleSelection = null;
        view.EligibleNobleIds.Clear();
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GameFinished e, SplendorBoardView view)
    {
        ClearTurnDeadline(view);
        view.Status = GameStatus.Finished;
        view.WinnerId = e.WinnerId;
        view.WinnerName = e.WinnerName;
        view.CurrentPlayerId = null;
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GameDeleted e, SplendorBoardView view)
    {
        ClearTurnDeadline(view);
        view.Status = GameStatus.Deleted;
        SetProjectionMetadata(view, e.Timestamp);
    }

    private static void ClearTurnDeadline(SplendorBoardView view)
    {
        view.TurnId = null;
        view.ExpiresAt = null;
    }

    private static void SetProjectionMetadata(SplendorBoardView view, DateTimeOffset timestamp)
    {
        view.UpdatedAt = timestamp;
        view.GameVersion++;
    }

    private static List<string> GetMarketForLevel(SplendorBoardView game, int level) => level switch
    {
        1 => game.Market1,
        2 => game.Market2,
        3 => game.Market3,
        _ => throw new ArgumentException($"Invalid market level: {level}")
    };

    private static void DecrementDeckCount(SplendorBoardView game, int level)
    {
        switch (level)
        {
            case 1: game.Deck1Count--; break;
            case 2: game.Deck2Count--; break;
            case 3: game.Deck3Count--; break;
            default: throw new ArgumentException($"Invalid deck level: {level}");
        }
    }
}
