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

    internal static IEnumerable<IDomainEvent> Decide(DeleteGameCommand command, SplendorGameState state)
    {
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game is already deleted.");

        var now = DateTimeOffset.UtcNow;
        var events = new List<IDomainEvent>
        {
            new GameDeleted(command.GameId, now)
        };

        if (state.Status != GameStatus.Finished)
        {
            events.AddRange(state.Players.Select(player =>
                (IDomainEvent)new PlayerParticipationEnded(command.GameId, player.Key, player.Value.OwnerId, now)));
        }

        return events;
    }
}
