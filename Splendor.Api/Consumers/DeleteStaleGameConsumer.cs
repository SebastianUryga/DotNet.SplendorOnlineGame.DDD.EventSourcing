using MassTransit;
using MediatR;
using Splendor.Application.Commands;
using Splendor.Contracts.Messages;
using Splendor.Domain.Rules;

namespace Splendor.Api.Consumers;

public class DeleteStaleGameConsumer : IConsumer<DeleteStaleGameMessage>
{
    private readonly IMediator _mediator;

    public DeleteStaleGameConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task Consume(ConsumeContext<DeleteStaleGameMessage> context) =>
        _mediator.Send(new DeleteGameCommand(context.Message.GameId, Caller.System), context.CancellationToken);
}
