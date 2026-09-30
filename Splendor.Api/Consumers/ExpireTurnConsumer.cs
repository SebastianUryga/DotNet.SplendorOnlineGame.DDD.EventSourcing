using MassTransit;
using MediatR;
using Splendor.Application.Commands;
using Splendor.Contracts.Messages;

namespace Splendor.Api.Consumers;

public class ExpireTurnConsumer : IConsumer<ExpireTurnMessage>
{
    private readonly IMediator _mediator;

    public ExpireTurnConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task Consume(ConsumeContext<ExpireTurnMessage> context) =>
        _mediator.Send(
            new ExpireTurnCommand(context.Message.GameId, context.Message.TurnId, context.Message.PlayerId),
            context.CancellationToken);
}
