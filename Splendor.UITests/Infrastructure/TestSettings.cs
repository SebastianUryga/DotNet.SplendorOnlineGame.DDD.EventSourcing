namespace Splendor.UITests.Infrastructure;

public static class TestSettings
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("SPLENDOR_UI_URL") ?? "http://localhost:4200";

    // Any value - TestAuthHandler doesn't validate the token, only checks if it exists
    public static string TestToken =>
        Environment.GetEnvironmentVariable("SPLENDOR_TEST_TOKEN") ?? "ui-test-token";
}
