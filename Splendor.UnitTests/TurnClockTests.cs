using Splendor.Domain;
using Splendor.Domain.Rules;
using Splendor.Domain.ValueObjects;
using FluentAssertions;
using Splendor.Application.Commands;
using Splendor.Application.DecisionStates;
using Splendor.Application.Snapshots;
using Splendor.Domain.Common;
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
            Caller = Caller.User(ownerId),
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

        var events = Expire(gameId, clock, playerId, state, now);

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

    [Fact]
    public void ExpireTurn_ReturnsGemsAutomatically_WhenGemReturnIsPending()
    {
        var now = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        var (gameId, _, _, playerId, nextPlayerId, history) = TestHelpers.CreateStartedGame(nobles: new List<string>());
        var taken = new GemCollection(3, 2, 2, 2, 2, 0);
        history.Add(new GemsTaken(gameId, playerId, taken, now));
        history.Add(new GemsOverflowDetected(gameId, playerId, taken, 1, now));
        var state = new SplendorGameState();
        TestHelpers.ApplyHistory(state, history);
        var clock = StartedClock(gameId, playerId, now.AddSeconds(-1));

        var events = Expire(gameId, clock, playerId, state, now);

        events[0].Should().BeOfType<TurnExpired>();
        var resolved = events.OfType<GemLimitResolved>().Should().ContainSingle().Subject;
        resolved.ReturnedGems.Should().Be(new GemCollection(1, 0, 0, 0, 0, 0));
        events.OfType<TurnEnded>().Should().ContainSingle(e => e.PlayerId == playerId);
        events.OfType<TurnStarted>().Should().ContainSingle(e => e.PlayerId == nextPlayerId);
        state.PendingGemReturnPlayerId.Should().BeNull();
        state.Players[playerId].Gems.Total.Should().BeLessThanOrEqualTo(SplendorRules.MaxPlayerGems);
    }

    [Fact]
    public void ExpireTurn_PicksNobleAutomatically_WhenNobleSelectionIsPending()
    {
        var now = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        var (gameId, _, _, playerId, nextPlayerId, history) = TestHelpers.CreateStartedGame();
        var eligible = NobleDefinitions.AllNobles.Select(n => n.Id).Take(2).ToList();
        history.Add(new NobleSelectionRequired(gameId, playerId, eligible, now));
        var state = new SplendorGameState();
        TestHelpers.ApplyHistory(state, history);
        var clock = StartedClock(gameId, playerId, now.AddSeconds(-1));

        var events = Expire(gameId, clock, playerId, state, now);

        events.OfType<NobleAcquired>().Should().ContainSingle(e => e.NobleId == eligible.Order().First());
        events.OfType<TurnEnded>().Should().ContainSingle(e => e.PlayerId == playerId);
        events.OfType<TurnStarted>().Should().ContainSingle(e => e.PlayerId == nextPlayerId);
    }

    [Fact]
    public void ChooseGemsToReturn_ReturnsMostNumerousColourFirstAndGoldLast()
    {
        SplendorRules.ChooseGemsToReturn(new GemCollection(4, 3, 2, 1, 1, 1))
            .Should().Be(new GemCollection(2, 0, 0, 0, 0, 0));
        SplendorRules.ChooseGemsToReturn(new GemCollection(0, 0, 0, 0, 0, 12))
            .Should().Be(new GemCollection(0, 0, 0, 0, 0, 2));
        SplendorRules.ChooseGemsToReturn(new GemCollection(1, 1, 1, 1, 1, 7))
            .Should().Be(new GemCollection(1, 1, 0, 0, 0, 0));
    }

    [Fact]
    public void ExpireTurn_TakesTwoSteps_WhenGemReturnIsFollowedByNobleSelection()
    {
        var now = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        var (gameId, _, _, playerId, nextPlayerId, history) = TestHelpers.CreateStartedGame();
        foreach (var bonus in new[] { GemType.Diamond, GemType.Sapphire, GemType.Emerald, GemType.Ruby, GemType.Onyx })
            TestHelpers.AddPurchasedCardsWithBonus(history, gameId, playerId, bonus, 4);
        var taken = new GemCollection(3, 2, 2, 2, 2, 0);
        history.Add(new GemsTaken(gameId, playerId, taken, now));
        history.Add(new GemsOverflowDetected(gameId, playerId, taken, 1, now));
        var state = new SplendorGameState();
        TestHelpers.ApplyHistory(state, history);
        var clock = StartedClock(gameId, playerId, now.AddSeconds(-1));

        // first expiry: gems are returned, the player now has to choose between several nobles
        var first = Expire(gameId, clock, playerId, state, now);

        first.OfType<GemLimitResolved>().Should().ContainSingle();
        var selection = first.OfType<NobleSelectionRequired>().Should().ContainSingle().Subject;
        selection.EligibleNobleIds.Should().HaveCountGreaterThan(1);
        first.OfType<TurnEnded>().Should().BeEmpty();
        var deadline = first.OfType<TurnDeadlineStarted>().Should().ContainSingle(e => e.PlayerId == playerId).Subject;

        // second expiry, on the fresh deadline: a noble is picked and the turn ends
        history.AddRange(first);
        var afterFirst = new SplendorGameState();
        TestHelpers.ApplyHistory(afterFirst, history);
        var secondClock = new TurnClockState();
        secondClock.Apply(deadline);

        var second = Expire(gameId, secondClock, playerId, afterFirst, deadline.ExpiresAt);

        second.OfType<NobleAcquired>().Should().ContainSingle(e => e.NobleId == selection.EligibleNobleIds.Order().First());
        second.OfType<NobleSelectionRequired>().Should().BeEmpty();
        second.OfType<TurnEnded>().Should().ContainSingle(e => e.PlayerId == playerId);
        second.OfType<TurnStarted>().Should().ContainSingle(e => e.PlayerId == nextPlayerId);
        afterFirst.Players[playerId].Gems.Total.Should().BeLessThanOrEqualTo(SplendorRules.MaxPlayerGems);
    }

    // Mirrors ExpireTurnCommandHandler.Handle: decide, apply to local state, then complete the turn.
    private static List<IDomainEvent> Expire(
        Guid gameId, TurnClockState clock, string playerId, SplendorGameState state, DateTimeOffset now)
    {
        var events = ExpireTurnCommandHandler.Decide(
            new ExpireTurnCommand(gameId, clock.TurnId, playerId), state, clock, now).ToList();
        state.Apply(events);
        events.AddRange(ExpireTurnCommandHandler.DecideCompletion(events, state, clock, now));
        return events;
    }

    private static TurnClockState StartedClock(Guid gameId, string playerId, DateTimeOffset expiresAt)
    {
        var clock = new TurnClockState();
        clock.Apply(new TurnDeadlineStarted(
            gameId, Guid.NewGuid(), playerId, expiresAt, expiresAt.AddMinutes(-1)));
        return clock;
    }
}
