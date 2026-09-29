using Marten;
using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.Events;
using Splendor.Application.Snapshots;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record JoinGameCommand : IAuthoredCommand, IRequest
{
    public Guid GameId { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public class JoinGameCommandHandler : IRequestHandler<JoinGameCommand>
{
    private readonly IDocumentSession _session;

    public JoinGameCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task Handle(JoinGameCommand command, CancellationToken cancellationToken)
    {
        var stream = await _session.Events.FetchForWriting<SplendorGameState>(command.GameId, cancellationToken);
        var state = stream.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state);

        stream.AppendMany(events.Select(e => _session.TagEvent(e)));
        await _session.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<IDomainEvent> Decide(JoinGameCommand command, SplendorGameState state)
    {
        if (state.Status == GameStatus.Started) throw new InvalidOperationException("Game is already started.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game is already finished.");
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game has been deleted.");
        if (state.Players.Count >= 4) throw new InvalidOperationException("Game full.");
        if (state.Players.Values.Any(player => string.Equals(player.Name, command.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Player with name '{command.Name}' already exists in this game.");

        var playerId = Guid.NewGuid() + " " + command.Name;
        return new List<IDomainEvent>
        {
            new PlayerJoined(command.GameId, playerId, command.OwnerId, command.Name, DateTimeOffset.UtcNow)
        };
    }
}
