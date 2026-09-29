using FluentAssertions;
using Splendor.Application.Commands;
using Splendor.Application.DecisionStates;
using Splendor.Application.Snapshots;
using Splendor.Domain.Events;
using Xunit;

namespace Splendor.UnitTests;

public class LeaveGameTests
{
    [Fact]
    public void PlayerCanLeaveLobby()
    {
        var gameId = Guid.NewGuid();
        var state = new SplendorGameState();
        state.Apply(new GameCreated(gameId, "owner-1", DateTimeOffset.UtcNow));
        state.Apply(new PlayerJoined(gameId, "player-1", "owner-1", "Alice", DateTimeOffset.UtcNow));

        var left = LeaveGameCommandHandler.Decide(new LeaveGameCommand
        {
            GameId = gameId,
            OwnerId = "owner-1"
        }, state).Should().BeOfType<PlayerLeft>().Subject;

        state.Apply(left);
        var ownerState = new JoinGameDecisionState();
        ownerState.Apply(new PlayerJoined(gameId, "player-1", "owner-1", "Alice", DateTimeOffset.UtcNow));
        ownerState.Apply(left);

        state.Players.Should().BeEmpty();
        state.PlayerOrder.Should().BeEmpty();
        ownerState.ActiveGameIds.Should().BeEmpty();
    }

    [Fact]
    public void PlayerCannotLeaveStartedGame()
    {
        var setup = TestHelpers.CreateStartedGame();
        var state = new SplendorGameState();
        TestHelpers.ApplyHistory(state, setup.History);

        var act = () => LeaveGameCommandHandler.Decide(new LeaveGameCommand
        {
            GameId = setup.GameId,
            OwnerId = setup.Owner1
        }, state);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("You can only leave a game before it starts.");
    }
}
