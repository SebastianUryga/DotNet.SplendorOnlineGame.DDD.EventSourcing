using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Splendor.Api.Auth;

public class RateLimitSettings
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
    public string PartitionBy { get; set; } = "ip";
}

public static class RateLimitingExtensions
{
    private const string GlobalPolicy = "Global";

    /// <summary>
    /// Registers one named policy per entry of the RateLimits config section (defaults below, overridable in config).
    /// New limit = new entry + [EnableRateLimiting("Name")] on the endpoint.
    /// "Global" is not a named policy: it is the safety net applied to every request, so an endpoint
    /// without the attribute is still limited.
    /// </summary>
    public static IServiceCollection AddConfiguredRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var limits = new Dictionary<string, RateLimitSettings>
        {
            ["GuestAuth"] = new() { PermitLimit = 3, WindowSeconds = 600, PartitionBy = "ip" },
            ["GameActions"] = new() { PermitLimit = 10, WindowSeconds = 5, PartitionBy = "user" },
            [GlobalPolicy] = new() { PermitLimit = 60, WindowSeconds = 10, PartitionBy = "user" }
        };
        configuration.GetSection("RateLimits").Bind(limits);

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            RateLimitPartition<string> Partition(HttpContext context, RateLimitSettings limit)
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var key = limit.PartitionBy == "user"
                    ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ip
                    : ip;
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit.PermitLimit,
                    Window = TimeSpan.FromSeconds(limit.WindowSeconds)
                });
            }

            var global = limits[GlobalPolicy];
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => Partition(context, global));

            foreach (var (name, limit) in limits.Where(l => l.Key != GlobalPolicy))
            {
                options.AddPolicy(name, context => Partition(context, limit));
            }
        });
    }
}
