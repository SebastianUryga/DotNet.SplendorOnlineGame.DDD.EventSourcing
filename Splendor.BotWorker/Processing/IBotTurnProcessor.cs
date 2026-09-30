namespace Splendor.BotWorker.Processing;

public interface IBotTurnProcessor
{
    Task ProcessAsync(Guid gameId, CancellationToken cancellationToken);
}
