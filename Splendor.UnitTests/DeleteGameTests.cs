using FluentAssertions;
using Splendor.Application.Commands;
using Splendor.Application.Snapshots;
using Splendor.Domain.Events;
using Xunit;

namespace Splendor.UnitTests;

public class DeleteGameTests
{
    [Fact]
    public void DeletingFinishedGameDoesNotEndParticipationAgain()
    {
        var gameId = Guid.NewGuid();
        var state = new SplendorGameState();
        state.Apply(new GameCreated(gameId, "owner-1", DateTimeOffset.UtcNow));
        state.Apply(new PlayerJoined(gameId, "player-1", "owner-1", "Alice", DateTimeOffset.UtcNow));
        state.Apply(new GameFinished(gameId, "player-1", "owner-1", "Alice", 15, DateTimeOffset.UtcNow));

        var events = DeleteGameCommandHandler.Decide(new DeleteGameCommand(gameId, "owner-1"), state).ToList();

        events.Should().ContainSingle(@event => @event is GameDeleted);
        events.Should().NotContain(@event => @event is PlayerParticipationEnded);
    }

    [Fact]
    public void OnlyCreatorCanDeleteGame()
    {
        var gameId = Guid.NewGuid();
        var state = new SplendorGameState();
        state.Apply(new GameCreated(gameId, "owner-1", DateTimeOffset.UtcNow));

        var act = () => DeleteGameCommandHandler.Decide(new DeleteGameCommand(gameId, "someone-else"), state).ToList();

        act.Should().Throw<InvalidOperationException>();
    }
}
