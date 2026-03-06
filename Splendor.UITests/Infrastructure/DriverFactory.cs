using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace Splendor.UITests.Infrastructure;

public static class DriverFactory
{
    public static IWebDriver CreateChromeDriver()
    {
        new DriverManager().SetUpDriver(new ChromeConfig(), WebDriverManager.Helpers.VersionResolveStrategy.MatchingBrowser);

        var options = new ChromeOptions();
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");

        return new ChromeDriver(options);
    }
}
