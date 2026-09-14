using JasperFx.Events.Aggregation;
using JasperFx.Events.Tags;
using Splendor.Application.Events;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.DecisionStates;

[BoundaryAggregate]
internal class DeleteGameDecisionState
{
    public GameStatus Status { get; private set; }

    public void Apply(GameCreated _)
    {
        Status = GameStatus.Created;
    }

    public void Apply(GameDeleted _)
    {
        Status = GameStatus.Deleted;
    }
}
