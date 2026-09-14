using Marten;
using JasperFx.Events;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Microsoft.Extensions.DependencyInjection;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Infrastructure.Events;
using Splendor.Infrastructure.Projections;
using Splendor.Application;
using Splendor.Application.Events;

namespace Splendor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string martenConnectionString)
    {
        services.AddMarten(options =>
        {
            options.Connection(martenConnectionString);

            // Allow re-creating database (DEV only)
            options.AutoCreateSchemaObjects = JasperFx.AutoCreate.All;

            // Events configuration
            options.Events.StreamIdentity = StreamIdentity.AsGuid;
            options.Events.RegisterTagType<GameTag>("game");
            options.Events.RegisterTagType<OwnerTag>("owner");
            options.Events.TagEventsBy(@event => @event switch
            {
                GameCreated created => [new GameTag(created.GameId), new OwnerTag(created.CreatorId)],
                PlayerJoined joined => [new GameTag(joined.GameId), new OwnerTag(joined.OwnerId)],
                IDomainEvent domainEvent => [new GameTag(domainEvent.GameId)],
                _ => []
            });

            // Projections
            options.Projections.Add<GameSummaryProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<SplendorBoardProjection>(ProjectionLifecycle.Inline);
        })
        .UseLightweightSessions()
        .AddAsyncDaemon(DaemonMode.HotCold)
        .AddSubscriptionWithServices<GameEventProcessor>(ServiceLifetime.Scoped, o =>
        {
            // Start the subscription at the most current "high water mark" of the
            // event store. This effectively makes the subscription a "hot"
            // observable that only sees events when the subscription is active
            o.Options.SubscribeFromPresent();
        });

        services.AddScoped<EventPublisher>();

        return services;
    }
}
