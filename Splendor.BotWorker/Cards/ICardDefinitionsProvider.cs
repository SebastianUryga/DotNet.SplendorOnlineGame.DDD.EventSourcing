using Splendor.Domain.ValueObjects;

namespace Splendor.BotWorker.Cards;

public interface ICardDefinitionsProvider
{
    Task<Dictionary<string, Card>> GetCardsAsync(CancellationToken cancellationToken);
}