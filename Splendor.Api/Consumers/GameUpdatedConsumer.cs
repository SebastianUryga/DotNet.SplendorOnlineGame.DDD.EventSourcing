using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Splendor.Api.Hubs;
using Splendor.Application.Messages;
using Splendor.Application.Queries;
using MediatR;

namespace Splendor.Api.Consumers;

public class GameUpdatedConsumer : IConsumer<Batch<GameUpdatedMessage>>
{
    private readonly IHubContext<GameHub> _hubContext;
    private readonly IMediator _mediator;

    public GameUpdatedConsumer(IHubContext<GameHub> hubContext, IMediator mediator)
    {
        _hubContext = hubContext;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<Batch<GameUpdatedMessage>> context)
    {
        // Group by GameId to process each game only once per batch
        var uniqueGames = context.Message
            .Select(m => m.Message)
            .GroupBy(m => m.GameId)
            .Select(g => g.First()); // We just need the ID to trigger a refresh

        foreach (var message in uniqueGames)
        {
            // Get latest GameView
            var gameView = await _mediator.Send(new GetGameQuery(message.GameId));

            if (gameView != null)
            {
                // Send to all clients in the game group
                await _hubContext.Clients
                    .Group(message.GameId.ToString())
                    .SendAsync("GameUpdated", gameView);
            }
        }
    }
}
