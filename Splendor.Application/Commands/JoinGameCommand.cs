using Marten;
using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.DecisionStates;
using Splendor.Application.Events;
using Splendor.Application.Snapshots;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;
using Splendor.Domain.Rules;

namespace Splendor.Application.Commands;

public record JoinGameCommand : IAuthorizedCommand, IRequest
{
    public Guid GameId { get; init; }
    public required Caller Caller { get; init; }
    public required PlayerName Name { get; init; }
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
        var boundary = await _session.Events.FetchForWritingByTags<JoinGameDecisionState>(
            JoinGameDecisionState.Query(command.Caller.UserId.Value), cancellationToken);
        var ownerState = boundary.Aggregate ?? new JoinGameDecisionState();

        var events = Decide(command, state, ownerState);

        stream.AppendMany(events.Select(e => _session.TagEvent(e)));
        await _session.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<IDomainEvent> Decide(JoinGameCommand command, SplendorGameState state, JoinGameDecisionState ownerState)
    {
        var name = command.Name.Value;
        if (state.Status == GameStatus.Started) throw new InvalidOperationException("Game is already started.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game is already finished.");
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game has been deleted.");
        if (state.Players.Count >= 4) throw new InvalidOperationException("Game full.");
        //if (state.Players.Values.Any(player => player.PlayerOwnerId == command.Caller.UserId.Value))
        //    throw new InvalidOperationException("You already control a player in this game.");
        if (!PlatformRules.CanJoinAnotherGame(ownerState.ActiveGameIds.Count))
            throw new InvalidOperationException($"You cannot be active in more than {PlatformRules.MaxActiveGames} games.");
        if (state.Players.Values.Any(player => string.Equals(player.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Player with name '{name}' already exists in this game.");

        var playerId = Guid.NewGuid() + " " + name;
        return new List<IDomainEvent>
        {
            new PlayerJoined(command.GameId, playerId, command.Caller.UserId.Value, name, DateTimeOffset.UtcNow)
        };
    }
}
