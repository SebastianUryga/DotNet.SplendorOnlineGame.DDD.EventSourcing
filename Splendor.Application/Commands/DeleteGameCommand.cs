using Marten;
using MediatR;
using Splendor.Application.Events;
using Splendor.Application.Snapshots;
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
        var stream = await _session.Events.FetchForWriting<SplendorGameState>(command.GameId, cancellationToken);
        var state = stream.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state);

        stream.AppendMany(events.Select(e => _session.TagEvent(e)));
        await _session.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<IDomainEvent> Decide(DeleteGameCommand command, SplendorGameState state)
    {
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game is already deleted.");

        return new List<IDomainEvent>
        {
            new GameDeleted(command.GameId, DateTimeOffset.UtcNow)
        };
    }
}
