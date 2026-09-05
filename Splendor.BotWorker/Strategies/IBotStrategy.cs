using Splendor.Domain.ValueObjects;

namespace Splendor.BotWorker.Strategies;

public interface IBotStrategy
{
    BotAction? ChooseMove(GameView game, string botPlayerId, IReadOnlyDictionary<string, Card> cards);
}