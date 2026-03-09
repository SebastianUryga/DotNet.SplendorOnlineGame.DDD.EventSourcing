using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Splendor.UITests.Infrastructure;

public abstract class TestBase : IDisposable
{
    protected readonly IWebDriver Driver;
    protected readonly WebDriverWait Wait;

    protected TestBase()
    {
        Driver = DriverFactory.CreateChromeDriver();
        Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
        SetAuthToken();
    }

    // Uses the existing input in the application header (app.component.ts)
    // exactly like a real user pasting a token
    private void SetAuthToken()
    {
        Driver.Navigate().GoToUrl(TestSettings.BaseUrl);
        var initialWait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
        var input = initialWait.Until(d => d.FindElements(By.CssSelector("[data-testid='token-input']")).FirstOrDefault());
        input.SendKeys(TestSettings.TestToken);
        Driver.FindElement(By.CssSelector("[data-testid='set-token-btn']")).Click();
    }

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }
}
