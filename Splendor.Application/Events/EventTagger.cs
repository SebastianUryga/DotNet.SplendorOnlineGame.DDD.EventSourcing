using System;
using JasperFx.Events;
using Marten;
using Splendor.Domain.Common;
using Splendor.Domain.Events;

namespace Splendor.Application.Events;

/// <summary>The only place that tags events. GameDeleted/GameFinished do not carry the creator, so handlers pass it in.</summary>
public static class EventTagger
{
    public static IEvent TagEvent(this IDocumentSession session, IDomainEvent @event, string? creatorId = null)
    {
        var tagged = session.Events.BuildEvent(@event);

        tagged.WithTag(new GameTag(@event.GameId));

        switch (@event)
        {
            case GameCreated created:
                tagged.WithTag(new GameCreatorTag(created.CreatorId));
                break;
            case GameDeleted or GameFinished:
                tagged.WithTag(new GameCreatorTag(creatorId
                    ?? throw new ArgumentNullException(nameof(creatorId), $"{@event.GetType().Name} must be tagged with the game creator.")));
                break;
            case PlayerJoined joined:
                tagged.WithTag(new PlayerOwnerTag(joined.OwnerId));
                break;
            case PlayerLeft left:
                tagged.WithTag(new PlayerOwnerTag(left.OwnerId));
                break;
            case PlayerParticipationEnded ended:
                tagged.WithTag(new PlayerOwnerTag(ended.OwnerId));
                break;
        }

        return tagged;
    }
}
