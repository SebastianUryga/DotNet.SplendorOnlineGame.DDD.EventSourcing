using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Splendor.IntegrationTests;

public class BasicTests : IClassFixture<SplendorApiFactory>
{
    private readonly SplendorApiFactory _factory;

    public BasicTests(SplendorApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Cards_ReturnsSuccessAndCorrectContentType()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/cards");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8", 
            response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task Create_Game_ReturnsCreatedStatus()
    {
        // Arrange
        var client = _factory.CreateClient();
        var content = new StringContent("{\"OwnerId\":\"test-user-id\"}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/games", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("id", responseBody);
    }
}
