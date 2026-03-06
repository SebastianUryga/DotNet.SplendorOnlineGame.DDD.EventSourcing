using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Splendor.UITests.Pages;

public class GamePage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public GamePage(IWebDriver driver, WebDriverWait wait)
    {
        _driver = driver;
        _wait = wait;
    }

    public void WaitForLoad()
        => _wait.Until(d => d.FindElement(By.CssSelector("[data-testid='game-container']")));

    public bool IsLoaded()
        => _driver.FindElements(By.CssSelector("[data-testid='game-container']")).Count > 0;

    public bool IsTakeGemsButtonEnabled()
    {
        var btn = _driver.FindElement(By.CssSelector("[data-testid='take-gems-btn']"));
        return btn.Enabled && btn.GetAttribute("disabled") == null;
    }

    public void ClickQuit()
        => _driver.FindElement(By.CssSelector("[data-testid='quit-game-btn']")).Click();
}
