// DISABLED for now, see HardDeleteGameCommand.cs.
//
// using MassTransit;
// using MediatR;
// using Splendor.Application.Commands;
// using Splendor.Contracts.Messages;
//
// namespace Splendor.Api.Consumers;
//
// public class HardDeleteGameConsumer : IConsumer<HardDeleteGameMessage>
// {
//     private readonly IMediator _mediator;
//
//     public HardDeleteGameConsumer(IMediator mediator)
//     {
//         _mediator = mediator;
//     }
//
//     public Task Consume(ConsumeContext<HardDeleteGameMessage> context) =>
//         _mediator.Send(new HardDeleteGameCommand(context.Message.GameId), context.CancellationToken);
// }
