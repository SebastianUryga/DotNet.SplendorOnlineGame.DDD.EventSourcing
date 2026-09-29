using Marten;
using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.DecisionStates;
using Splendor.Application.Events;
using Splendor.Application.Snapshots;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record LeaveGameCommand : IAuthoredCommand, IRequest
{
    public Guid GameId { get; init; }
    public string OwnerId { get; init; } = string.Empty;
}

public class LeaveGameCommandHandler : IRequestHandler<LeaveGameCommand>
{
    private readonly IDocumentSession _session;

    public LeaveGameCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task Handle(LeaveGameCommand command, CancellationToken cancellationToken)
    {
        var stream = await _session.Events.FetchForWriting<SplendorGameState>(command.GameId, cancellationToken);
        var state = stream.Aggregate ?? throw new InvalidOperationException("Game not found.");
        await _session.Events.FetchForWritingByTags<JoinGameDecisionState>(
            JoinGameDecisionState.Query(command.OwnerId), cancellationToken);

        stream.AppendOne(_session.TagEvent(Decide(command, state)));
        await _session.SaveChangesAsync(cancellationToken);
    }

    internal static IDomainEvent Decide(LeaveGameCommand command, SplendorGameState state)
    {
        if (state.Status != GameStatus.Created)
            throw new InvalidOperationException("You can only leave a game before it starts.");

        var player = state.Players.SingleOrDefault(player => player.Value.OwnerId == command.OwnerId);
        if (player.Key is null)
            throw new InvalidOperationException("You do not control a player in this game.");

        return new PlayerLeft(command.GameId, player.Key, command.OwnerId, DateTimeOffset.UtcNow);
    }
}
