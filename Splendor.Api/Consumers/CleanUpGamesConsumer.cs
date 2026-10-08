using MassTransit;
using MediatR;
using Splendor.Application.Queries;
using Splendor.Contracts.Messages;
using Splendor.Domain.Rules;

namespace Splendor.Api.Consumers;

public class CleanUpGamesConsumer : IConsumer<CleanUpGamesMessage>
{
    private readonly IMediator _mediator;
    private readonly TimeProvider _timeProvider;

    public CleanUpGamesConsumer(IMediator mediator, TimeProvider timeProvider)
    {
        _mediator = mediator;
        _timeProvider = timeProvider;
    }

    public async Task Consume(ConsumeContext<CleanUpGamesMessage> context)
    {
        var now = _timeProvider.GetUtcNow();

        var stale = await _mediator.Send(new GetGamesQuery(UpdatedBefore: now - PlatformRules.StaleGameAge), context.CancellationToken);
        await context.PublishBatch(stale.Select(game => new DeleteStaleGameMessage(game.Id)), context.CancellationToken);

        // DISABLED for now, see HardDeleteGameCommand.cs.
        // For deleted games UpdatedAt is the deletion time (set by GameSummaryProjection).
        // var deleted = await _mediator.Send(
        //     new GetGamesQuery(IncludeDeleted: true, UpdatedBefore: now - PlatformRules.DeletedGameRetention), context.CancellationToken);
        // await context.PublishBatch(
        //     deleted.Where(game => game.Status == nameof(GameStatus.Deleted)).Select(game => new HardDeleteGameMessage(game.Id)),
        //     context.CancellationToken);
    }
}
