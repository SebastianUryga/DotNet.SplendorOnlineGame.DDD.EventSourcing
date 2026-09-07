using MassTransit;
using Splendor.BotWorker.Processing;
using Splendor.Contracts.Messages;

namespace Splendor.BotWorker.Messaging;

public class BotGameUpdatedConsumer : IConsumer<Batch<GameUpdatedMessage>>
{
    private readonly IBotTurnProcessor _processor;
    private readonly IBotGameMembershipHandler _botGameMembershipHandler;

    public BotGameUpdatedConsumer(IBotTurnProcessor processor, IBotGameMembershipHandler botGameMembershipHandler)
    {
        _processor = processor;
        _botGameMembershipHandler = botGameMembershipHandler;
    }

    public async Task Consume(ConsumeContext<Batch<GameUpdatedMessage>> context)
    {
        var latestMessagesByGame = context.Message
            .Select(x => x.Message)
            .GroupBy(x => x.GameId)
            .Select(g => g.OrderByDescending(x => x.Version).First());

        foreach (var message in latestMessagesByGame)
        {
            if (message.EventType == GameEventTypes.PlayerInvited)
            {
                await _botGameMembershipHandler.HandleInvitationAsync(message.GameId, context.CancellationToken);
                continue;
            }

            await _processor.ProcessAsync(message.GameId, message.Version, context.CancellationToken);
        }
    }
}
