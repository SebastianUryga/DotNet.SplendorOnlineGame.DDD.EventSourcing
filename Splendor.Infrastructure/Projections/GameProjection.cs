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
        });

        ProjectEvent<PlayerJoined>((view, e) => {
            view.Players.Add(new PlayerView { Id = e.PlayerId, Name = e.Name });
        });

        ProjectEvent<GameStarted>((view, e) => {
             view.Status = "Started";
             // Initialize generic market for MVP
             view.MarketGems = new GemCollection(4,4,4,4,4,5);
             if (view.Players.Any()) view.CurrentPlayerId = view.Players.First().Id;
        });

        ProjectEvent<TurnStarted>((view, e) => {
            view.CurrentPlayerId = e.PlayerId;
        });

        ProjectEvent<GemsTaken>((view, e) => {
            view.MarketGems -= e.Gems;
            var p = view.Players.FirstOrDefault(x => x.Id == e.PlayerId);
            if (p != null) p.Gems += e.Gems;
        });
    }
}
