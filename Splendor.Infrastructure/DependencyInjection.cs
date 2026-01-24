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
        .AddSubscriptionWithServices<GameEventPublisher>(ServiceLifetime.Scoped)
        .AddSubscriptionWithServices<GameReadModelSubscription>(ServiceLifetime.Scoped);

        services.AddScoped<IEventStore, MartenEventStore>();

        services.AddDbContext<ReadModelsContext>(options =>
            options.UseSqlServer(readModelsConnectionString));

        services.AddScoped<IReadModelsContext>(provider => provider.GetRequiredService<ReadModelsContext>());

        return services;
    }
}
