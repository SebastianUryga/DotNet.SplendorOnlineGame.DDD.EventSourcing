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
            .Select(g => g.OrderByDescending(x => x.StreamVersion).First())
            .ToList();

        // Games run concurrently, so a delayed move in one game does not hold up the others
        await Task.WhenAll(latestMessagesByGame.Select(message => HandleAsync(message, context.CancellationToken)));
    }

    private Task HandleAsync(GameUpdatedMessage message, CancellationToken cancellationToken) =>
        message.EventType == GameEventTypes.PlayerInvited
            ? _botGameMembershipHandler.HandleInvitationAsync(message.GameId, message.Data, cancellationToken)
            : _processor.ProcessAsync(message.GameId, cancellationToken);
}
