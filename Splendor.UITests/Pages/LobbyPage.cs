using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Splendor.UITests.Infrastructure;

namespace Splendor.UITests.Pages;

public class LobbyPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public LobbyPage(IWebDriver driver, WebDriverWait wait)
    {
        _driver = driver;
        _wait = wait;
    }

    public string GetGameId()
        => _driver.Url.Split('/')[^2]; // /games/{id}/lobby

    public void NavigateTo(string gameId)
        => _driver.Navigate().GoToUrl(TestSettings.BaseUrl + $"/games/{gameId}/lobby");

    public void WaitForLoad()
        => _wait.Until(d => d.FindElements(By.CssSelector("[data-testid='join-btn']")).FirstOrDefault());

    public void EnterPlayerName(string name)
    {
        var input = _wait.Until(d => d.FindElements(By.CssSelector("[data-testid='player-name-input']")).FirstOrDefault());
        input.Clear();
        input.SendKeys(name);
    }

    public void ClickJoin()
        => _driver.FindElement(By.CssSelector("[data-testid='join-btn']")).Click();

    public void ClickStart()
        => _driver.FindElement(By.CssSelector("[data-testid='start-btn']")).Click();

    public void ClickBackToList()
        => _driver.FindElement(By.CssSelector("[data-testid='back-to-list-btn']")).Click();

    public int PlayerCount()
        => _driver.FindElements(By.CssSelector("[data-testid='player-name']")).Count;

    public string GameStatus()
        => _driver.FindElement(By.CssSelector("[data-testid='game-status']")).Text;

    public bool IsStartButtonEnabled()
    {
        var btn = _driver.FindElements(By.CssSelector("[data-testid='start-btn']"));
        if (btn.Count == 0) return false;
        return btn[0].Enabled;
    }
}
