using JasperFx.Events.Aggregation;
using JasperFx.Events.Tags;
using Splendor.Application.Events;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.DecisionStates;

[BoundaryAggregate]
internal class JoinGameDecisionState
{
    public GameStatus Status { get; private set; }
    public List<string> PlayerNames { get; } = new();

    public static EventTagQuery Query(Guid gameId) =>
        new EventTagQuery()
            .Or<GameCreated, GameTag>(new GameTag(gameId))
            .Or<PlayerJoined, GameTag>(new GameTag(gameId))
            .Or<GameStarted, GameTag>(new GameTag(gameId))
            .Or<GameFinished, GameTag>(new GameTag(gameId))
            .Or<GameDeleted, GameTag>(new GameTag(gameId));

    public void Apply(GameCreated _)
    {
        Status = GameStatus.Created;
    }

    public void Apply(PlayerJoined e)
    {
        PlayerNames.Add(e.Name);
    }

    public void Apply(GameStarted _)
    {
        Status = GameStatus.Started;
    }

    public void Apply(GameFinished _)
    {
        Status = GameStatus.Finished;
    }

    public void Apply(GameDeleted _)
    {
        Status = GameStatus.Deleted;
    }
}
