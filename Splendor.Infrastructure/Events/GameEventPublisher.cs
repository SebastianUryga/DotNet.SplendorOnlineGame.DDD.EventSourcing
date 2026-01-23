using Marten;
using Marten.Events;
using Marten.Events.Daemon;
using Marten.Events.Daemon.Internals;
using Marten.Subscriptions;
using MassTransit;
using Splendor.Application.Messages;
using Splendor.Domain.Events;

namespace Splendor.Infrastructure.Events;

public class GameEventPublisher : SubscriptionBase
{
    private readonly IPublishEndpoint _publishEndpoint;

    public GameEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;

        // Filter only game events
        IncludeType<GameCreated>();
        IncludeType<PlayerJoined>();
        IncludeType<GameStarted>();
        IncludeType<TurnStarted>();
        IncludeType<GemsTaken>();
        IncludeType<CardPurchased>();
        IncludeType<CardRevealed>();
        IncludeType<TurnEnded>();
    }

    public override async Task<IChangeListener> ProcessEventsAsync(
        EventRange page,
        ISubscriptionController controller,
        IDocumentOperations operations,
        CancellationToken ct)
    {
        foreach (var @event in page.Events)
        {
            var message = new GameUpdatedMessage(
                GameId: @event.StreamId,
                EventType: @event.EventTypeName,
                Version: @event.Version
            );

            await _publishEndpoint.Publish(message, ct);
        }

        return NullChangeListener.Instance;
    }
}
