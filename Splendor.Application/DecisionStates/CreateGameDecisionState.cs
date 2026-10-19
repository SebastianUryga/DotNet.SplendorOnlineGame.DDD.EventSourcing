using JasperFx.Events.Aggregation;
using JasperFx.Events.Tags;
using Splendor.Application.Events;
using Splendor.Domain.Events;

namespace Splendor.Application.DecisionStates;

[BoundaryAggregate]
internal class CreateGameDecisionState
{
    public HashSet<Guid> OpenGameIds { get; } = new();

    /// <summary>Every game this creator ever made, deleted or finished ones included.</summary>
    public int TotalCreated { get; private set; }

    public static EventTagQuery Query(string creatorId) =>
        new EventTagQuery()
            .Or<GameCreated, GameCreatorTag>(new GameCreatorTag(creatorId))
            .Or<GameDeleted, GameCreatorTag>(new GameCreatorTag(creatorId))
            .Or<GameFinished, GameCreatorTag>(new GameCreatorTag(creatorId));

    public void Apply(GameCreated e)
    {
        OpenGameIds.Add(e.GameId);
        TotalCreated++;
    }

    public void Apply(GameDeleted e) => OpenGameIds.Remove(e.GameId);

    public void Apply(GameFinished e) => OpenGameIds.Remove(e.GameId);
}
