using Marten;
using Marten.Events;
using Marten.Events.Daemon;
using Marten.Events.Daemon.Internals;
using Marten.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Splendor.Infrastructure.Persistence;
using Splendor.Domain.Events;

namespace Splendor.Infrastructure.Events;

public class GameEventProcessor : SubscriptionBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    public GameEventProcessor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

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
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var projector = scope.ServiceProvider.GetRequiredService<GameReadModelProjector>();
        var publisher = scope.ServiceProvider.GetRequiredService<EventPublisher>();

        foreach (var @event in page.Events)
        {
            // 1. Update Read Model
            await projector.ProjectAsync(@event.Data, cancellationToken);

            // 2. Publish Event Notification
            await publisher.PublishAsync(@event, cancellationToken);
        }

        return NullChangeListener.Instance;
    }
}
