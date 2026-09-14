using Marten.Events.Projections;
using Splendor.Application.ReadModels;
using Splendor.Domain.Events;

namespace Splendor.Infrastructure.Projections;

public partial class GameSummaryProjection : MultiStreamProjection<GameSummaryView, Guid>
{
    public GameSummaryProjection()
    {
        Identity<GameCreated>(e => e.GameId);
        Identity<PlayerJoined>(e => e.GameId);
        Identity<PlayerInvited>(e => e.GameId);
        Identity<GameStarted>(e => e.GameId);
        Identity<TurnStarted>(e => e.GameId);
        Identity<GameFinished>(e => e.GameId);
        Identity<GameDeleted>(e => e.GameId);
    }

    public GameSummaryView Create(GameCreated e) => new()
    {
        Id = e.GameId,
        Status = "Created",
        UpdatedAt = e.Timestamp,
        Version = 1
    };

    public void Apply(PlayerJoined e, GameSummaryView view)
    {
        view.PlayerCount++;
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(PlayerInvited e, GameSummaryView view) =>
        SetProjectionMetadata(view, e.Timestamp);

    public void Apply(GameStarted e, GameSummaryView view)
    {
        view.Status = "Started";
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(TurnStarted e, GameSummaryView view)
    {
        view.CurrentPlayerId = e.PlayerId;
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GameFinished e, GameSummaryView view)
    {
        view.Status = "Finished";
        view.CurrentPlayerId = null;
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GameDeleted e, GameSummaryView view)
    {
        view.Status = "Deleted";
        SetProjectionMetadata(view, e.Timestamp);
    }

    private static void SetProjectionMetadata(GameSummaryView view, DateTimeOffset timestamp)
    {
        view.UpdatedAt = timestamp;
        view.Version++;
    }
}
