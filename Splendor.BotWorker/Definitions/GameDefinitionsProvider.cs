using Splendor.BotWorker.Api;
using Splendor.Domain.ValueObjects;

namespace Splendor.BotWorker.Cards;

public class GameDefinitionsProvider : IGameDefinitionsProvider
{
    private readonly IGameApiClient _gameApiClient;
    private Dictionary<string, Card>? _cards;
    private Dictionary<string, Noble>? _nobles;

    public GameDefinitionsProvider(IGameApiClient gameApiClient)
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

    public async Task<Dictionary<string, Noble>> GetNoblesAsync(CancellationToken cancellationToken)
    {
        if (_nobles is not null)
        {
            return _nobles;
        }

        var nobles = await _gameApiClient.GetNoblesAsync(cancellationToken);

        _nobles = nobles.ToDictionary(noble => noble.Id);

        return _nobles;
    }
}