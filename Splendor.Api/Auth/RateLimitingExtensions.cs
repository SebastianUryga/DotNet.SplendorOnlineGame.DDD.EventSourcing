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
    /// <summary>
    /// Registers one named policy per entry of the RateLimits config section (defaults below, overridable in config).
    /// New limit = new entry + [EnableRateLimiting("Name")] on the endpoint.
    /// </summary>
    public static IServiceCollection AddConfiguredRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var limits = new Dictionary<string, RateLimitSettings>
        {
            ["GuestAuth"] = new() { PermitLimit = 3, WindowSeconds = 600, PartitionBy = "ip" },
            ["GameActions"] = new() { PermitLimit = 10, WindowSeconds = 5, PartitionBy = "user" }
        };
        configuration.GetSection("RateLimits").Bind(limits);

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            foreach (var (name, limit) in limits)
            {
                options.AddPolicy(name, context =>
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
                });
            }
        });
    }
}
