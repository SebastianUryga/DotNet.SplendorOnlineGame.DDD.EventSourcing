using FluentAssertions;
using Splendor.Application.DecisionStates;
using Splendor.Domain.Events;
using Xunit;

namespace Splendor.UnitTests;

public class CreateGameDecisionStateTests
{
    [Fact]
    public void TracksOpenGamesUntilDeletedOrFinished()
    {
        var state = new CreateGameDecisionState();
        var (open, deleted, finished) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        foreach (var id in new[] { open, deleted, finished }) state.Apply(new GameCreated(id, "owner-1", now));
        state.Apply(new GameDeleted(deleted, now));
        state.Apply(new GameFinished(finished, "player-1", "owner-1", "Alice", 15, now));

        state.OpenGameIds.Should().Equal(open);
    }
}
