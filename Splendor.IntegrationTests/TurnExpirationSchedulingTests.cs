using FluentAssertions;
using MassTransit;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Splendor.Contracts.Messages;
using Splendor.Domain.Events;

namespace Splendor.IntegrationTests;

public class TurnExpirationSchedulingTests : IClassFixture<SplendorApiFactory>
{
    private readonly SplendorApiFactory _factory;
    private readonly IDocumentStore _store;

    public TurnExpirationSchedulingTests(SplendorApiFactory factory)
    {
        _factory = factory;
        _store = factory.Services.GetRequiredService<IDocumentStore>();
    }

    [Fact]
    public async Task ScheduledMessage_InvokesExpireTurnCommand()
    {
        var game = new GameTestHelper(_store);
        var gameId = await game.SeedStartedGameAsync();
        var turnId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddMilliseconds(250);
        await game.AppendAsync(gameId,
            new TurnDeadlineStarted(gameId, turnId, "player-1", expiresAt, DateTimeOffset.UtcNow));

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var scheduler = scope.ServiceProvider.GetRequiredService<IMessageScheduler>();
            await scheduler.SchedulePublish(
                expiresAt.UtcDateTime,
                new ExpireTurnMessage(gameId, turnId, "player-1"));
        }

        var timeout = DateTimeOffset.UtcNow.AddSeconds(10);
        var expired = false;
        while (DateTimeOffset.UtcNow < timeout)
        {
            await using var session = _store.QuerySession();
            if ((await session.Events.FetchStreamAsync(gameId)).Any(e => e.Data is TurnExpired))
            {
                expired = true;
                break;
            }

            await Task.Delay(100);
        }

        expired.Should().BeTrue();
    }
}
