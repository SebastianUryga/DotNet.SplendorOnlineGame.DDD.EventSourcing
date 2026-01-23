using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Splendor.Api.Hubs;
using Splendor.Application.Messages;
using Splendor.Application.Queries;
using MediatR;
using Splendor.Application.ReadModels;

namespace Splendor.Api.Consumers;

public class GameUpdatedConsumer : IConsumer<GameUpdatedMessage>
{
    private readonly IHubContext<GameHub> _hubContext;
    private readonly IMediator _mediator;

    public GameUpdatedConsumer(IHubContext<GameHub> hubContext, IMediator mediator)
    {
        _hubContext = hubContext;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<GameUpdatedMessage> context)
    {
        var message = context.Message;

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
