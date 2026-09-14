using JasperFx.Events.Tags;
using Marten;
using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.DecisionStates;
using Splendor.Application.Events;
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
        var query = JoinGameDecisionState.Query(command.GameId);
        var boundary = await _session.Events.FetchForWritingByTags<JoinGameDecisionState>(query, cancellationToken);
        var state = boundary.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state);

        boundary.AppendMany(events.Select(e => _session.TagEvent(e)).ToArray());
        await _session.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<IDomainEvent> Decide(JoinGameCommand command, JoinGameDecisionState state)
    {
        if (state.Status == GameStatus.Started) throw new InvalidOperationException("Game is already started.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game is already finished.");
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game has been deleted.");
        if (state.PlayerNames.Count >= 4) throw new InvalidOperationException("Game full.");
        if (state.PlayerNames.Contains(command.Name, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Player with name '{command.Name}' already exists in this game.");

        var playerId = Guid.NewGuid() + " " + command.Name;
        return new List<IDomainEvent>
        {
            new PlayerJoined(command.GameId, playerId, command.OwnerId, command.Name, DateTimeOffset.UtcNow)
        };
    }
}
