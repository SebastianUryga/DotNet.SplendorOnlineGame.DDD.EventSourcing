using Splendor.Domain.ValueObjects;

namespace Splendor.BotWorker.Cards;

public interface IGameDefinitionsProvider
{
    Task<Dictionary<string, Card>> GetCardsAsync(CancellationToken cancellationToken);
    Task<Dictionary<string, Noble>> GetNoblesAsync(CancellationToken cancellationToken);
}