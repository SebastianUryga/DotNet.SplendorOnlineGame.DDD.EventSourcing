using System.Text.Json;
using FluentAssertions;
using Splendor.Api.Consumers;
using Splendor.Contracts.Messages;
using Xunit;

namespace Splendor.IntegrationTests;

public class GameNotificationTests
{
    // The event as the consumer receives it: serialized domain event in GameUpdatedMessage.Data.
    private static GameNotification Notify(string eventType, object domainEvent) =>
        GameUpdatedConsumer.ToNotification(
            new GameUpdatedMessage(Guid.NewGuid(), eventType, 1, JsonSerializer.Serialize(domainEvent)));

    [Fact]
    public void CardPurchased_CarriesCardIdAndPaidGems()
    {
        var notification = Notify("card_purchased", new
        {
            PlayerId = "p1",
            CardId = "L1-01",
            PaidGems = new { Diamond = 1, Sapphire = 0, Emerald = 2, Ruby = 0, Onyx = 0, Gold = 0 }
        });

        var json = JsonSerializer.Serialize(notification, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        notification.PlayerId.Should().Be("p1");
        json.Should().Contain("\"cardId\":\"L1-01\"").And.Contain("\"paidGems\":{\"diamond\":1");
    }

    [Fact]
    public void CardReserved_DoesNotLeakCardId()
    {
        var notification = Notify("card_reserved", new { PlayerId = "p1", CardId = "L3-07" });

        notification.PlayerId.Should().Be("p1");
        notification.Data.Should().BeNull();
    }
}
