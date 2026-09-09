using JasperFx.Events;
using JasperFx.Events.Projections;
using Marten;

namespace Splendor.DcbSpike;

public static class MartenDcb
{
    public static IDocumentStore CreateStore(string connectionString) =>
        DocumentStore.For(options =>
        {
            options.Connection(connectionString);
            options.AutoCreateSchemaObjects = JasperFx.AutoCreate.All;

            var game = options.Events.RegisterTagType<GameTag>("game");
            options.Events.RegisterTagType<OwnerTag>("owner");
            game.ForAggregate<StartGameDecisionState>();
            game.ForAggregate<TakeGemsDecisionState>();
            game.ForAggregate<BuyCardDecisionState>();
            game.ForAggregate<ReserveCardDecisionState>();
            game.ForAggregate<TurnCompletionDecisionState>();

            options.Projections.Add<GameSummaryProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<SplendorBoardProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<UserStatsProjection>(ProjectionLifecycle.Inline);
        });

    public static IEvent Tag(this IDocumentSession session, object @event)
    {
        var tagged = session.Events.BuildEvent(@event);

        if (@event is IDomainEvent domainEvent)
        {
            tagged.WithTag(new GameTag(domainEvent.GameId));

            if (@event is PlayerJoined joined)
            {
                tagged.WithTag(new OwnerTag(joined.OwnerId));
            }

            if (@event is GameFinished finished)
            {
                tagged.WithTag(new OwnerTag(finished.WinnerOwnerId));
            }

            return tagged;
        }

        throw new ArgumentOutOfRangeException(nameof(@event), @event.GetType().Name, "Event has no DCB tags.");
    }
}
