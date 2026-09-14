using FluentAssertions;
using Splendor.Application.Commands;
using Splendor.Domain;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.IntegrationTests;

public class ChooseNobleCommandHandlerTests : IClassFixture<SplendorApiFactory>
{
    private readonly GameTestHelper _game;

    public ChooseNobleCommandHandlerTests(SplendorApiFactory factory)
    {
        _game = new GameTestHelper(factory.Services.GetRequiredService<Marten.IDocumentStore>());
    }

    [Fact]
    public async Task Handle_AcquiresSelectedNoble_WhenNobleSelectionIsPending()
    {
        var gameId = await _game.SeedStartedGameAsync();

        var bonusCards = CardDefinitions.AllCards
            .Where(card => card.BonusType is GemType.Diamond or GemType.Sapphire or GemType.Emerald)
            .GroupBy(card => card.BonusType)
            .SelectMany(cards => cards.Take(3));
        var events = bonusCards
            .Select(card => (IDomainEvent)new CardPurchased(gameId, "player-1", card.Id, GemCollection.Empty, DateTimeOffset.UtcNow))
            .Append(new NobleSelectionRequired(gameId, "player-1", ["N_01"], DateTimeOffset.UtcNow))
            .ToArray();
        await _game.AppendAsync(gameId, events);

        await _game.ExecuteAsync(session => new ChooseNobleCommandHandler(session).Handle(new ChooseNobleCommand
        {
            GameId = gameId,
            OwnerId = "owner-1",
            PlayerId = "player-1",
            NobleId = "N_01"
        }, CancellationToken.None));

        var state = await _game.LoadStateAsync(gameId);
        state.Should().NotBeNull();
        state!.PendingNobleSelectionPlayerId.Should().BeNull();
        state.EligibleNobleIds.Should().BeEmpty();
        state.Players["player-1"].OwnedNobleIds.Should().Contain("N_01");
    }

}
