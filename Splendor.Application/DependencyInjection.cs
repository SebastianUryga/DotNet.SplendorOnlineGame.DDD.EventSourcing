using Microsoft.Extensions.DependencyInjection;
using Splendor.Application.Behaviors;
using System.Reflection;

namespace Splendor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(EventStoreRetryBehavior<,>));
        });
        return services;
    }
}
