using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Splendor.UITests.Infrastructure;

namespace Splendor.UITests.Pages;

public class GamesListPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public GamesListPage(IWebDriver driver, WebDriverWait wait)
    {
        _driver = driver;
        _wait = wait;
    }

    public void NavigateTo()
        => _driver.Navigate().GoToUrl(TestSettings.BaseUrl + "/games");

    public void WaitForLoad()
        => _wait.Until(d => d.FindElements(By.CssSelector("[data-testid='create-game-btn']")).FirstOrDefault());

    public void CreateNewGame()
        => _driver.FindElement(By.CssSelector("[data-testid='create-game-btn']")).Click();

    public int GameCount()
        => _driver.FindElements(By.CssSelector("[data-testid='game-card']")).Count;

    public void ClickFirstGame()
        => _driver.FindElements(By.CssSelector("[data-testid='open-game-btn']")).First().Click();

    public void ClickGame(string gameId)
    {
        var prefix = gameId[..6];
        var card = _driver.FindElements(By.CssSelector("[data-testid='game-card']"))
            .First(c => c.FindElement(By.TagName("h3")).Text.Contains(prefix));
        card.FindElement(By.CssSelector("[data-testid='open-game-btn']")).Click();
    }

    public bool HasNoGamesMessage()
        => _driver.FindElements(By.CssSelector("[data-testid='no-games-message']")).Count > 0;
}
