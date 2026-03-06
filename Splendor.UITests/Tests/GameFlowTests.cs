using FluentAssertions;
using Splendor.UITests.Infrastructure;
using Splendor.UITests.Pages;
using Xunit;

namespace Splendor.UITests.Tests;

public class GameFlowTests : TestBase
{
    [Fact]
    public void GamesListPage_Loads_Successfully()
    {
        var page = new GamesListPage(Driver, Wait);
        page.NavigateTo();
        page.WaitForLoad();

        Driver.Url.Should().Contain("/games");
    }

    [Fact]
    public void CreateGame_NavigatesToLobby()
    {
        var page = new GamesListPage(Driver, Wait);
        page.NavigateTo();
        page.WaitForLoad();
        page.CreateNewGame();

        var lobby = new LobbyPage(Driver, Wait);
        lobby.WaitForLoad();

        Driver.Url.Should().Contain("/lobby");
    }

    [Fact]
    public void JoinGame_ShowsPlayerInList()
    {
        var gamesPage = new GamesListPage(Driver, Wait);
        gamesPage.NavigateTo();
        gamesPage.WaitForLoad();
        gamesPage.CreateNewGame();

        var lobby = new LobbyPage(Driver, Wait);
        lobby.WaitForLoad();
        lobby.EnterPlayerName("TestPlayer");
        lobby.ClickJoin();

        Wait.Until(_ => lobby.PlayerCount() >= 1);

        lobby.PlayerCount().Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void StartGame_RequiresTwoPlayers_ButtonDisabledWithOne()
    {
        var gamesPage = new GamesListPage(Driver, Wait);
        gamesPage.NavigateTo();
        gamesPage.WaitForLoad();
        gamesPage.CreateNewGame();

        var lobby = new LobbyPage(Driver, Wait);
        lobby.WaitForLoad();
        lobby.EnterPlayerName("SoloPlayer");
        lobby.ClickJoin();

        Wait.Until(_ => lobby.PlayerCount() >= 1);

        lobby.IsStartButtonEnabled().Should().BeFalse();
    }

    [Fact]
    public void NavigateBackToList_FromLobby()
    {
        var gamesPage = new GamesListPage(Driver, Wait);
        gamesPage.NavigateTo();
        gamesPage.WaitForLoad();
        gamesPage.CreateNewGame();

        var lobby = new LobbyPage(Driver, Wait);
        lobby.WaitForLoad();
        lobby.ClickBackToList();

        gamesPage.WaitForLoad();
        Driver.Url.Should().Contain("/games");
    }
}
