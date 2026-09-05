using MassTransit;
using Splendor.BotWorker.Processing;
using Splendor.Contracts.Messages;

namespace Splendor.BotWorker.Messaging;

public class BotGameUpdatedConsumer : IConsumer<Batch<GameUpdatedMessage>>
{
    private readonly IBotTurnProcessor _processor;

    public BotGameUpdatedConsumer(IBotTurnProcessor processor)
    {
        _processor = processor;
    }

    public async Task Consume(ConsumeContext<Batch<GameUpdatedMessage>> context)
    {
        var latestMessagesByGame = context.Message
            .Select(x => x.Message)
            .GroupBy(x => x.GameId)
            .Select(g => g.OrderByDescending(x => x.Version).First());

        foreach (var message in latestMessagesByGame)
        {
            await _processor.ProcessAsync(
                message.GameId,
                message.Version,
                context.CancellationToken);
        }
    }
}
