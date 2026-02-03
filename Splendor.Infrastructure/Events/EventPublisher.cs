using Marten.Events;
using MassTransit;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.Messages;

namespace Splendor.Infrastructure.Events;

public class EventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public EventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync(IEvent @event, CancellationToken ct)
    {
        var message = new GameUpdatedMessage(
            GameId: @event.StreamId,
            EventType: @event.EventTypeName,
            Version: @event.Version
        );

        await _publishEndpoint.Publish(message, ct);
    }
}
