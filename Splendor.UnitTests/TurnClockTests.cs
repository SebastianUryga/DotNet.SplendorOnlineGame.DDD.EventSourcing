using FluentAssertions;
using Splendor.Application.Commands;
using Splendor.Application.DecisionStates;
using Splendor.Application.Snapshots;
using Splendor.Domain.Events;
using Xunit;

namespace Splendor.UnitTests;

public class TurnClockTests
{
    [Fact]
    public void TakeGems_RejectsActionAfterDeadline()
    {
        var now = DateTimeOffset.UtcNow;
        var (gameId, ownerId, _, playerId, _, history) = TestHelpers.CreateStartedGame();
        var state = new SplendorGameState();
        TestHelpers.ApplyHistory(state, history);
        var clock = StartedClock(gameId, playerId, now.AddSeconds(-1));

        var act = () => TakeGemsCommandHandler.Decide(new TakeGemsCommand
        {
            GameId = gameId,
            OwnerId = ownerId,
            PlayerId = playerId,
            Diamond = 1,
            Sapphire = 1,
            Emerald = 1
        }, state, clock);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Turn deadline has passed.");
    }

    [Fact]
    public void ExpireTurn_EndsCurrentTurnAndStartsNextTurn()
    {
        var now = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        var (gameId, _, _, playerId, nextPlayerId, history) = TestHelpers.CreateStartedGame();
        var state = new SplendorGameState();
        TestHelpers.ApplyHistory(state, history);
        var clock = StartedClock(gameId, playerId, now.AddSeconds(-1));

        var events = ExpireTurnCommandHandler.Decide(
            new ExpireTurnCommand(gameId, clock.TurnId, playerId), state, clock, now);

        events.OfType<TurnExpired>().Should().ContainSingle();
        events.OfType<TurnEnded>().Should().ContainSingle(e => e.PlayerId == playerId);
        events.OfType<TurnStarted>().Should().ContainSingle(e => e.PlayerId == nextPlayerId);
        events.OfType<TurnDeadlineStarted>().Should().ContainSingle(e =>
            e.PlayerId == nextPlayerId && e.TurnId != clock.TurnId);
    }

    [Fact]
    public void ExpireTurn_IgnoresStaleTurnId()
    {
        var now = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        var (gameId, _, _, playerId, _, history) = TestHelpers.CreateStartedGame();
        var state = new SplendorGameState();
        TestHelpers.ApplyHistory(state, history);
        var clock = StartedClock(gameId, playerId, now.AddSeconds(-1));

        var events = ExpireTurnCommandHandler.Decide(
            new ExpireTurnCommand(gameId, Guid.NewGuid(), playerId), state, clock, now);

        events.Should().BeEmpty();
    }

    [Fact]
    public void ExpireTurn_IgnoresTimeoutAfterPlayerStartedAction()
    {
        var now = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        var (gameId, _, _, playerId, _, history) = TestHelpers.CreateStartedGame();
        var state = new SplendorGameState();
        TestHelpers.ApplyHistory(state, history);
        var clock = StartedClock(gameId, playerId, now.AddSeconds(30));
        clock.Apply(new GemsTaken(gameId, playerId, new(1, 1, 1, 0, 0, 0), now));

        var events = ExpireTurnCommandHandler.Decide(
            new ExpireTurnCommand(gameId, clock.TurnId, playerId), state, clock, now.AddMinutes(1));

        events.Should().BeEmpty();
    }

    private static TurnClockState StartedClock(Guid gameId, string playerId, DateTimeOffset expiresAt)
    {
        var clock = new TurnClockState();
        clock.Apply(new TurnDeadlineStarted(
            gameId, Guid.NewGuid(), playerId, expiresAt, expiresAt.AddMinutes(-1)));
        return clock;
    }
}
