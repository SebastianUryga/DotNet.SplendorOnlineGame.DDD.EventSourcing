using Marten;
using Marten.Events.Daemon.Resiliency;
using Microsoft.Extensions.DependencyInjection;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain.Aggregates;
using Splendor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Marten.Events.Projections;
using Splendor.Infrastructure.Events;
using Splendor.Infrastructure.Projections;

namespace Splendor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string martenConnectionString, string readModelsConnectionString)
    {
        services.AddMarten(options =>
        {
            options.Connection(martenConnectionString);

            // Allow re-creating database (DEV only)
            options.AutoCreateSchemaObjects = Weasel.Core.AutoCreate.All;

            // Events configuration
            options.Events.StreamIdentity = Marten.Events.StreamIdentity.AsGuid;

            // Projections
            options.Projections.Add<GameProjection>(ProjectionLifecycle.Inline);
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

        services.AddScoped<GameReadModelProjector>();
        services.AddScoped<EventPublisher>();
        services.AddScoped<IEventStore, MartenEventStore>();

        services.AddDbContext<ReadModelsContext>(options =>
            options.UseSqlServer(readModelsConnectionString));

        services.AddScoped<IReadModelsContext>(provider => provider.GetRequiredService<ReadModelsContext>());

        return services;
    }
}
