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

        ProjectEvent<CardPurchased>((view, e) => {
            view.Version++;
            var player = view.Players.FirstOrDefault(x => x.Id == e.PlayerId);
            if (player != null)
            {
                player.OwnedCardIds.Add(e.CardId);
                player.Gems -= e.PaidGems;
            }

            view.MarketGems += e.PaidGems;
            var card = Splendor.Domain.CardDefinitions.GetById(e.CardId);
            if (card != null)
            {
                GetMarketForLevel(view, card.Level).Remove(e.CardId);
            }
        });

        ProjectEvent<CardRevealed>((view, e) => {
            view.Version++;
            GetMarketForLevel(view, e.Level).Add(e.CardId);
            DecrementDeckCount(view, e.Level);
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
