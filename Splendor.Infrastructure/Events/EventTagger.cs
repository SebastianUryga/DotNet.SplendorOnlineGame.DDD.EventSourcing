using JasperFx.Events;
using Marten;
using Splendor.Application;
using Splendor.Application.Events;
using Splendor.Domain.Common;
using Splendor.Domain.Events;

namespace Splendor.Infrastructure.Events;

public static class EventTagger
{
    public static IEvent TagEvent(this IDocumentSession session, IDomainEvent @event)
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
