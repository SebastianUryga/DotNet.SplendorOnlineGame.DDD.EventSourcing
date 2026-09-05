using Splendor.BotWorker.Api;
using Splendor.Domain.ValueObjects;

namespace Splendor.BotWorker.Cards;

public class CardDefinitionsProvider : ICardDefinitionsProvider
{
    private readonly IGameApiClient _gameApiClient;
    private Dictionary<string, Card>? _cards;

    public CardDefinitionsProvider(IGameApiClient gameApiClient)
    {
        _gameApiClient = gameApiClient;
    }

    public async Task<Dictionary<string, Card>> GetCardsAsync(CancellationToken cancellationToken)
    {
        if (_cards is not null)
        {
            return _cards;
        }

        var cards = await _gameApiClient.GetCardsAsync(cancellationToken);

        _cards = cards.ToDictionary(card => card.Id);

        return _cards;
    }
}