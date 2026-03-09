using FluentAssertions;
using Splendor.UITests.Infrastructure;
using Splendor.UITests.Pages;
using Xunit;

namespace Splendor.UITests.Tests;

public class GameFlowTests : TestBase
{
    [Fact]
    public void FullGameFlow()
    {
        // 1. Games list loads
        var gamesPage = new GamesListPage(Driver, Wait);
        gamesPage.NavigateTo();
        gamesPage.WaitForLoad();
        Driver.Url.Should().Contain("/games");

        // 2. Create game → navigates to lobby
        gamesPage.CreateNewGame();
        var lobby = new LobbyPage(Driver, Wait);
        lobby.WaitForLoad();
        Driver.Url.Should().Contain("/lobby");
        var gameId = lobby.GetGameId();

        // 3. Solo player → Start button disabled
        lobby.EnterPlayerName("Player1");
        lobby.ClickJoin();
        Wait.Until(_ => lobby.PlayerCount() >= 1);
        lobby.PlayerCount().Should().Be(1);
        lobby.IsStartButtonEnabled().Should().BeFalse();

        // 4. Second player joins via games list → Start button enabled
        lobby.ClickBackToList();
        gamesPage.WaitForLoad();
        gamesPage.ClickGame(gameId);
        lobby.WaitForLoad();
        lobby.EnterPlayerName("Player2");
        lobby.ClickJoin();
        Wait.Until(_ => lobby.PlayerCount() >= 2);
        lobby.PlayerCount().Should().Be(2);
        lobby.IsStartButtonEnabled().Should().BeTrue();

        // 5. Start game → navigates to game view
        lobby.ClickStart();
        var gamePage = new GamePage(Driver, Wait);
        gamePage.WaitForLoad();
        Driver.Url.Should().Contain("/play");

        // 6. Quit → game still on list
        gamePage.ClickQuit();
        gamesPage.WaitForLoad();
        gamesPage.GameCount().Should().BeGreaterOrEqualTo(1);
    }
}
