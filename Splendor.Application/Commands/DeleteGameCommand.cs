using JasperFx.Events.Tags;
using Marten;
using MediatR;
using Splendor.Application.DecisionStates;
using Splendor.Application.Events;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record DeleteGameCommand(Guid GameId) : IRequest;

public class DeleteGameCommandHandler : IRequestHandler<DeleteGameCommand>
{
    private readonly IDocumentSession _session;

    public DeleteGameCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task Handle(DeleteGameCommand command, CancellationToken cancellationToken)
    {
        var query = new EventTagQuery()
            .Or<GameCreated, GameTag>(new GameTag(command.GameId))
            .Or<GameDeleted, GameTag>(new GameTag(command.GameId));
        var boundary = await _session.Events.FetchForWritingByTags<DeleteGameDecisionState>(query, cancellationToken);
        var state = boundary.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state);

        boundary.AppendMany(events.Select(e => _session.TagEvent(e)).ToArray());
        await _session.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<IDomainEvent> Decide(DeleteGameCommand command, DeleteGameDecisionState state)
    {
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game is already deleted.");

        return new List<IDomainEvent>
        {
            new GameDeleted(command.GameId, DateTimeOffset.UtcNow)
        };
    }
}
