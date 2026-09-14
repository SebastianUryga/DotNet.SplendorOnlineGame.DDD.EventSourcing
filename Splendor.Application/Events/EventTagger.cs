using System;
using JasperFx.Events;
using Marten;
using Splendor.Domain.Common;
using Splendor.Domain.Events;

namespace Splendor.Application.Events;

public static class EventTagger
{
    public static IEvent TagEvent(this IDocumentSession session, IDomainEvent @event)
    {
        var tagged = session.Events.BuildEvent(@event);

        tagged.WithTag(new GameTag(@event.GameId));

        switch (@event)
        {
            case PlayerJoined joined:
                tagged.WithTag(new OwnerTag(joined.OwnerId));
                break;
            case GameFinished finished:
                tagged.WithTag(new OwnerTag(finished.WinnerOwnerId));
                break;      
        }

        return tagged;
    }
}
