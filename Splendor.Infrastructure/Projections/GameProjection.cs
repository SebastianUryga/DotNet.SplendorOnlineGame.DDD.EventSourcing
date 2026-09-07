using Marten.Events.Projections;
using Marten.Events.Aggregation;
using Splendor.Domain.Events;
using Splendor.Application.ReadModels;
using Splendor.Domain.ValueObjects;

namespace Splendor.Infrastructure.Projections;

public class GameProjection : SingleStreamProjection<GameView>
{
    // Projection builds a READ MODEL (GameView) optimized for UI/Querying.
    // It is separate from the Domain Aggregate (Write Model).
    // While logic often looks similar to Game.Apply, it serves a different purpose:
    // Game.Apply -> Internal consistency for validation.
    // GameProjection -> Public data structure for display.
    public GameProjection()
    {
        ProjectEvent<GameCreated>((view, e) => {
            view.Id = e.GameId;
            view.Status = "Created";
            view.Version = 1;
        });

        ProjectEvent<PlayerJoined>((view, e) => {
            view.Version++;
            view.Players.Add(new PlayerView 
            { 
                Id = e.PlayerId,       // String
                OwnerId = e.OwnerId,   // String
                Name = e.Name 
            });
        });

        ProjectEvent<PlayerInvited>((view, e) => {
            view.Version++;
        });

        ProjectEvent<GameStarted>((view, e) => {
             view.Version++;
             view.Status = "Started";
             view.MarketGems = new GemCollection(4, 4, 4, 4, 4, 5);
             view.Market1 = e.Market1.ToList();
             view.Market2 = e.Market2.ToList();
             view.Market3 = e.Market3.ToList();
             view.Deck1Count = e.Deck1.Count;
             view.Deck2Count = e.Deck2.Count;
             view.Deck3Count = e.Deck3.Count;
             view.Nobles = e.Nobles.ToList();
             if (view.Players.Any()) view.CurrentPlayerId = view.Players.First().Id;
        });

        ProjectEvent<TurnStarted>((view, e) => {
            view.Version++;
            view.CurrentPlayerId = e.PlayerId;
        });

        ProjectEvent<GemsTaken>((view, e) => {
            view.Version++;
            view.MarketGems -= e.Gems;
            var p = view.Players.FirstOrDefault(x => x.Id == e.PlayerId);
            if (p != null) p.Gems += e.Gems;
        });

        ProjectEvent<GemsOverflowDetected>((view, e) => {
            view.Version++;
            view.IsGemReturnPending = true;
        });

        ProjectEvent<CardPurchased>((view, e) => {
            view.Version++;
            var card = Splendor.Domain.CardDefinitions.GetById(e.CardId);
            var player = view.Players.FirstOrDefault(x => x.Id == e.PlayerId);
            if (player != null)
            {
                player.OwnedCardIds.Add(e.CardId);
                player.Gems -= e.PaidGems;
                if (card != null)
                {
                    player.PrestigePoints += card.PrestigePoints;
                }
                // Remove from reserved list if the player had reserved this card
                if (player.ReservedCardIds != null && player.ReservedCardIds.Contains(e.CardId))
                {
                    player.ReservedCardIds.Remove(e.CardId);
                }
            }

            view.MarketGems += e.PaidGems;
            if (card != null)
            {
                GetMarketForLevel(view, card.Level).Remove(e.CardId);
            }
        });

        ProjectEvent<GemLimitResolved>((view, e) => {
            view.Version++;
            var p = view.Players.FirstOrDefault(x => x.Id == e.PlayerId);
            if (p != null)
            {
                p.Gems -= e.ReturnedGems;
            }

            view.MarketGems += e.ReturnedGems;
            view.IsGemReturnPending = false;
        });

        ProjectEvent<CardRevealed>((view, e) => {
            view.Version++;
            GetMarketForLevel(view, e.Level).Add(e.CardId);
            DecrementDeckCount(view, e.Level);
        });

        ProjectEvent<CardReserved>((view, e) => {
            view.Version++;
            var player = view.Players.FirstOrDefault(x => x.Id == e.PlayerId);
            if (player != null)
            {
                player.ReservedCardIds ??= new();
                player.ReservedCardIds.Add(e.CardId);
            }

            var card = Splendor.Domain.CardDefinitions.GetById(e.CardId);
            if (card != null)
            {
                GetMarketForLevel(view, card.Level).Remove(e.CardId);
            }
        });

        ProjectEvent<NobleAcquired>((view, e) => {
            view.Version++;
            var player = view.Players.FirstOrDefault(x => x.Id == e.PlayerId);
            if (player != null)
            {
                player.OwnedNobleIds.Add(e.NobleId);
                player.PrestigePoints += Splendor.Domain.NobleDefinitions.GetById(e.NobleId)?.PrestigePoints ?? 0;
            }

            view.Nobles.Remove(e.NobleId);
            view.PlayerIdAwaitingNobleSelection = null;
            view.EligibleNobleIds.Clear();
        });

        ProjectEvent<NobleSelectionRequired>((view, e) => {
            view.Version++;
            view.PlayerIdAwaitingNobleSelection = e.PlayerId;
            view.EligibleNobleIds = e.EligibleNobleIds.ToList();
        });

        ProjectEvent<GameFinished>((view, e) => {
            view.Version++;
            view.Status = "Finished";
            view.WinnerId = e.WinnerId;
            view.WinnerName = e.WinnerName;
            view.CurrentPlayerId = null;
        });

        ProjectEvent<GameDeleted>((view, e) => {
            view.Version++;
            view.Status = "Deleted";
        });
    }

    private List<string> GetMarketForLevel(GameView game, int level)
    {
        return level switch
        {
            1 => game.Market1,
            2 => game.Market2,
            3 => game.Market3,
            _ => throw new ArgumentException($"Invalid market level: {level}")
        };
    }

    private void DecrementDeckCount(GameView game, int level)
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
