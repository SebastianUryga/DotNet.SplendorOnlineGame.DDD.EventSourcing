using JasperFx.Events.Aggregation;
using JasperFx.Events.Tags;
using Splendor.Application.Events;
using Splendor.Domain.Events;

namespace Splendor.Application.DecisionStates;

[BoundaryAggregate]
internal class JoinGameDecisionState
{
    public HashSet<Guid> ActiveGameIds { get; } = new();

    public static EventTagQuery Query(string ownerId) =>
        new EventTagQuery()
            .Or<PlayerJoined, OwnerTag>(new OwnerTag(ownerId))
            .Or<PlayerLeft, OwnerTag>(new OwnerTag(ownerId))
            .Or<PlayerParticipationEnded, OwnerTag>(new OwnerTag(ownerId));

    public void Apply(PlayerJoined e)
    {
        ActiveGameIds.Add(e.GameId);
    }

    public void Apply(PlayerParticipationEnded e)
    {
        ActiveGameIds.Remove(e.GameId);
    }

    public void Apply(PlayerLeft e)
    {
        ActiveGameIds.Remove(e.GameId);
    }
}
