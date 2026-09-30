using JasperFx.Events;
using MassTransit;
using Splendor.Contracts.Messages;
using Splendor.Domain.Common;
using Splendor.Domain.Events;

namespace Splendor.Infrastructure.Events;

public class EventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMessageScheduler _scheduler;

    public EventPublisher(IPublishEndpoint publishEndpoint, IMessageScheduler scheduler)
    {
        _publishEndpoint = publishEndpoint;
        _scheduler = scheduler;
    }

    public async Task PublishAsync(IEvent @event, CancellationToken ct)
    {
        var message = new GameUpdatedMessage(
            GameId: GetGameId(@event),
            EventType: @event.EventTypeName,
            StreamVersion: @event.Version
        );

        await _publishEndpoint.Publish(message, ct);

        if (@event.Data is TurnDeadlineStarted deadline)
        {
            await _scheduler.SchedulePublish(
                deadline.ExpiresAt.UtcDateTime,
                new ExpireTurnMessage(deadline.GameId, deadline.TurnId, deadline.PlayerId),
                ct);
        }
    }

    private static Guid GetGameId(IEvent @event) =>
        @event.Data is IDomainEvent domainEvent ? domainEvent.GameId : @event.StreamId;
}
