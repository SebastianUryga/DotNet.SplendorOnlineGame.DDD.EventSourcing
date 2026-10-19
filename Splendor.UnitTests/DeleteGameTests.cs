using FluentAssertions;
using Splendor.Application.Commands;
using Splendor.Application.Snapshots;
using Splendor.Domain.Events;
using Splendor.Domain.Rules;
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

        var events = DeleteGameCommandHandler.Decide(new DeleteGameCommand(gameId, Caller.User("owner-1")), state).ToList();

        events.Should().ContainSingle(@event => @event is GameDeleted);
        events.Should().NotContain(@event => @event is PlayerParticipationEnded);
    }

    [Fact]
    public void OnlyCreatorCanDeleteGame()
    {
        var gameId = Guid.NewGuid();
        var state = new SplendorGameState();
        state.Apply(new GameCreated(gameId, "owner-1", DateTimeOffset.UtcNow));

        var act = () => DeleteGameCommandHandler.Decide(new DeleteGameCommand(gameId, Caller.User("someone-else")), state).ToList();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AdminCanDeleteSomeoneElsesGame()
    {
        var gameId = Guid.NewGuid();
        var state = new SplendorGameState();
        state.Apply(new GameCreated(gameId, "owner-1", DateTimeOffset.UtcNow));

        var events = DeleteGameCommandHandler.Decide(new DeleteGameCommand(gameId, Caller.User("admin-1", Caller.AdminRole)), state).ToList();

        events.Should().ContainSingle(@event => @event is GameDeleted);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(PlatformRules.MaxActiveGames - 1, true)]
    [InlineData(PlatformRules.MaxActiveGames, false)]
    public void ActiveGamesLimit(int active, bool allowed) =>
        PlatformRules.CanJoinAnotherGame(Caller.User("user-1"), active).Should().Be(allowed);

    [Theory]
    [InlineData(PlatformRules.MaxActiveGames, true)]
    [InlineData(PlatformRules.MaxActiveGamesForBots - 1, true)]
    [InlineData(PlatformRules.MaxActiveGamesForBots, false)]
    public void BotActiveGamesLimit(int active, bool allowed) =>
        PlatformRules.CanJoinAnotherGame(Caller.User("bot-1", Caller.BotRole), active).Should().Be(allowed);

    [Theory]
    [InlineData(PlatformRules.MaxOpenCreatedGames - 1, true)]
    [InlineData(PlatformRules.MaxOpenCreatedGames, false)]
    public void OpenCreatedGamesLimit(int open, bool allowed) =>
        PlatformRules.CanCreateGame(Caller.User("user-1"), open).Should().Be(allowed);

    [Theory]
    [InlineData(PlatformRules.MaxOpenCreatedGames, true)]
    [InlineData(PlatformRules.MaxOpenCreatedGamesForBots - 1, true)]
    [InlineData(PlatformRules.MaxOpenCreatedGamesForBots, false)]
    public void BotOpenCreatedGamesLimit(int open, bool allowed) =>
        PlatformRules.CanCreateGame(Caller.User("bot-1", Caller.BotRole), open).Should().Be(allowed);

    [Theory]
    [InlineData(PlatformRules.MaxTotalCreatedGamesForGuests - 1, true)]
    [InlineData(PlatformRules.MaxTotalCreatedGamesForGuests, false)]
    public void GuestTotalCreatedGamesLimit(int total, bool allowed) =>
        PlatformRules.WithinTotalCreatedLimit(Caller.User("guest-1", Caller.GuestRole), total).Should().Be(allowed);

    [Fact]
    public void TotalCreatedGamesLimitDoesNotApplyToRegisteredUsers() =>
        PlatformRules.WithinTotalCreatedLimit(Caller.User("user-1"), 1000).Should().BeTrue();

    [Fact]
    public void GameIsStaleOnlyAfterStaleGameAge()
    {
        var now = DateTimeOffset.UtcNow;

        PlatformRules.IsStale(now - PlatformRules.StaleGameAge + TimeSpan.FromMinutes(1), now).Should().BeFalse();
        PlatformRules.IsStale(now - PlatformRules.StaleGameAge - TimeSpan.FromMinutes(1), now).Should().BeTrue();
    }

    [Fact]
    public void SystemCallerCanDeleteAnyGame()
    {
        var gameId = Guid.NewGuid();
        var state = new SplendorGameState();
        state.Apply(new GameCreated(gameId, "owner-1", DateTimeOffset.UtcNow));

        var events = DeleteGameCommandHandler.Decide(new DeleteGameCommand(gameId, Caller.System), state).ToList();

        events.Should().ContainSingle(@event => @event is GameDeleted);
    }
}
