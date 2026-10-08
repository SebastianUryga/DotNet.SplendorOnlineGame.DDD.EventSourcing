using MassTransit;
using Splendor.Contracts.Messages;

namespace Splendor.Api.Services;

/// <summary>Publishes CleanUpGamesMessage every hour. Runs only while the API is awake (Render Free sleeps).</summary>
public class GameCleanupTimer : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IBus _bus;

    public GameCleanupTimer(IBus bus)
    {
        _bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Short delay lets the app finish starting (and the async daemon catch up) after a cold start.
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        using var timer = new PeriodicTimer(Interval);
        do
        {
            await _bus.Publish(new CleanUpGamesMessage(), stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
