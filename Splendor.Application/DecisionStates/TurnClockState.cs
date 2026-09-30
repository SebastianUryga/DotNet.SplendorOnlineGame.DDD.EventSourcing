using JasperFx.Events.Aggregation;
using JasperFx.Events.Tags;
using Splendor.Application.Events;
using Splendor.Domain.Events;

namespace Splendor.Application.DecisionStates;

[BoundaryAggregate]
internal class TurnClockState
{
    private static readonly TimeSpan Duration = TimeSpan.FromMinutes(1);

    public Guid TurnId { get; private set; }
    public string PlayerId { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsWaitingForAction { get; private set; }

    public static EventTagQuery Query(Guid gameId) =>
        new EventTagQuery()
            .Or<TurnDeadlineStarted, GameTag>(new GameTag(gameId))
            .Or<GemsTaken, GameTag>(new GameTag(gameId))
            .Or<CardPurchased, GameTag>(new GameTag(gameId))
            .Or<CardReserved, GameTag>(new GameTag(gameId))
            .Or<TurnEnded, GameTag>(new GameTag(gameId))
            .Or<TurnExpired, GameTag>(new GameTag(gameId))
            .Or<GameFinished, GameTag>(new GameTag(gameId))
            .Or<GameDeleted, GameTag>(new GameTag(gameId));

    public static TurnDeadlineStarted Start(Guid gameId, string playerId, DateTimeOffset now) =>
        new(gameId, Guid.NewGuid(), playerId, now.Add(Duration), now);

    public bool CanStartAction(string playerId) =>
        IsWaitingForAction && PlayerId == playerId && DateTimeOffset.UtcNow < ExpiresAt;

    public void Apply(TurnDeadlineStarted e)
    {
        TurnId = e.TurnId;
        PlayerId = e.PlayerId;
        ExpiresAt = e.ExpiresAt;
        IsWaitingForAction = true;
    }

    public void Apply(GemsTaken _) => IsWaitingForAction = false;
    public void Apply(CardPurchased _) => IsWaitingForAction = false;
    public void Apply(CardReserved _) => IsWaitingForAction = false;
    public void Apply(TurnEnded _) => IsWaitingForAction = false;
    public void Apply(TurnExpired _) => IsWaitingForAction = false;
    public void Apply(GameFinished _) => IsWaitingForAction = false;
    public void Apply(GameDeleted _) => IsWaitingForAction = false;
}
