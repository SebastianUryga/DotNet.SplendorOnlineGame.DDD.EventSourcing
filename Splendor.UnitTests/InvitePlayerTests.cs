using FluentAssertions;
using Splendor.Application.Commands;
using Splendor.Application.Snapshots;
using Splendor.Domain.Events;
using Splendor.Domain.Rules;
using Splendor.Domain.ValueObjects;
using Xunit;

namespace Splendor.UnitTests;

public class InvitePlayerTests
{
    private static (Guid GameId, SplendorGameState State) NewLobby(bool creatorJoins)
    {
        var gameId = Guid.NewGuid();
        var state = new SplendorGameState();
        state.Apply(new GameCreated(gameId, "owner-1", DateTimeOffset.UtcNow));
        if (creatorJoins)
            state.Apply(new PlayerJoined(gameId, "player-1", "owner-1", "Alice", DateTimeOffset.UtcNow));
        return (gameId, state);
    }

    private static InvitePlayerCommand Invite(Guid gameId, string callerId) => new()
    {
        GameId = gameId,
        Caller = Caller.User(callerId),
        InviteeId = UserId.Create("bot-1")
    };

    [Fact]
    public void ParticipantCanInvite()
    {
        var (gameId, state) = NewLobby(creatorJoins: true);

        var invited = InvitePlayerCommandHandler.Decide(Invite(gameId, "owner-1"), state)
            .Should().ContainSingle().Which.Should().BeOfType<PlayerInvited>().Subject;

        invited.InviterId.Should().Be("owner-1");
        invited.InviteeId.Should().Be("bot-1");
    }

    [Fact]
    public void CreatorWhoIsNotAPlayerCanInvite()
    {
        var (gameId, state) = NewLobby(creatorJoins: false);

        var invited = InvitePlayerCommandHandler.Decide(Invite(gameId, "owner-1"), state)
            .Should().ContainSingle().Which.Should().BeOfType<PlayerInvited>().Subject;

        invited.InviterId.Should().Be("owner-1");
    }

    [Fact]
    public void StrangerCannotInvite()
    {
        var (gameId, state) = NewLobby(creatorJoins: true);

        var act = () => InvitePlayerCommandHandler.Decide(Invite(gameId, "stranger"), state).ToList();

        act.Should().Throw<InvalidOperationException>().WithMessage("Only the creator or a participant can invite.");
    }
}
