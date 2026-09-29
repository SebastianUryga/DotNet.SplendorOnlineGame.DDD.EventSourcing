using Marten;
using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.Events;
using Splendor.Application.Snapshots;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record InvitePlayerCommand : IAuthoredCommand, IRequest
{
    public Guid GameId { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string InviteeId { get; init; } = string.Empty;
}

public class InvitePlayerCommandHandler : IRequestHandler<InvitePlayerCommand>
{
    private readonly IDocumentSession _session;

    public InvitePlayerCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task Handle(InvitePlayerCommand command, CancellationToken cancellationToken)
    {
        var stream = await _session.Events.FetchForWriting<SplendorGameState>(command.GameId, cancellationToken);
        var state = stream.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state).ToList();

        stream.AppendMany(events.Select(e => _session.TagEvent(e)));
        await _session.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<IDomainEvent> Decide(InvitePlayerCommand command, SplendorGameState state)
    {
        if (state.Id == Guid.Empty) throw new InvalidOperationException("GameId missing in history");

        if (state.Status == GameStatus.Started) throw new InvalidOperationException("Game is already started.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game is already finished.");
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game has been deleted.");

        if (!state.Players.Values.Any(player => player.OwnerId == command.OwnerId))
            throw new InvalidOperationException("You do not control a player in this game");

        if (state.Players.Values.Any(player => player.OwnerId == command.InviteeId))
            throw new InvalidOperationException("Player already in game");

        yield return new PlayerInvited(state.Id, command.OwnerId, command.InviteeId, DateTimeOffset.UtcNow);
    }
}
