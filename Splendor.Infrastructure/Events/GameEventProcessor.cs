using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Marten.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
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
        IncludeType<PlayerInvited>();
        IncludeType<GameStarted>();
        IncludeType<TurnStarted>();
        IncludeType<GemsTaken>();
        IncludeType<CardPurchased>();
        IncludeType<CardRevealed>();
        IncludeType<TurnEnded>();
        IncludeType<GameFinished>();
        IncludeType<GameDeleted>();
        IncludeType<GemLimitResolved>();
        IncludeType<GemsOverflowDetected>();
        IncludeType<CardReserved>();
        IncludeType<NobleSelectionRequired>();
        IncludeType<NobleAcquired>();
    }

    public override async Task<IChangeListener> ProcessEventsAsync(
        EventRange page,
        ISubscriptionController controller,
        IDocumentOperations operations,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<EventPublisher>();

        foreach (var @event in page.Events)
        {
            await publisher.PublishAsync(@event, cancellationToken);
        }

        return NullChangeListener.Instance;
    }
}
