using JasperFx.Events;
using MassTransit;
using Splendor.Contracts.Messages;
using Splendor.Domain.Common;

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
            GameId: GetGameId(@event),
            EventType: @event.EventTypeName,
            Version: @event.Version
        );

        await _publishEndpoint.Publish(message, ct);
    }

    private static Guid GetGameId(IEvent @event) =>
        @event.Data is IDomainEvent domainEvent ? domainEvent.GameId : @event.StreamId;
}
