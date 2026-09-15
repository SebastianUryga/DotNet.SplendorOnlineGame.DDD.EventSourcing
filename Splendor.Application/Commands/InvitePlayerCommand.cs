using JasperFx.Events.Aggregation;
using JasperFx.Events.Daemon;
using JasperFx.Events.Tags;
using Marten;
using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.Events;
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
        var query = new EventTagQuery()
            .Or<GameCreated, GameTag>(new GameTag(command.GameId))
            .Or<PlayerJoined, GameTag>(new GameTag(command.GameId))
            .Or<GameStarted, GameTag>(new GameTag(command.GameId))
            .Or<GameFinished, GameTag>(new GameTag(command.GameId))
            .Or<GameDeleted, GameTag>(new GameTag(command.GameId));
        var boundary = await _session.Events.FetchForWritingByTags<PatiralGameState>(query, cancellationToken);
        var state = boundary.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state).ToList();


        // Tag and append events to the boundary
        boundary.AppendMany(events.Select(e => _session.TagEvent(e)).ToArray());
        await _session.SaveChangesAsync(cancellationToken);
    }

    private IEnumerable<IDomainEvent> Decide(InvitePlayerCommand command, PatiralGameState state)
    {
        if (state.GameId == Guid.Empty) throw new InvalidOperationException("GameId missing in history");

        // mirror Game.EnsureActive(): cannot invite after start/finish/delete
        if (state.GameStatus == GameStatus.Started) throw new InvalidOperationException("Game is already started.");
        if (state.GameStatus == GameStatus.Finished) throw new InvalidOperationException("Game is already finished.");
        if (state.GameStatus == GameStatus.Deleted) throw new InvalidOperationException("Game has been deleted.");

        // inviter must control a player in this game
        if (!state.JoinedPlayers.Any(p => p.OwnerId == command.OwnerId))
            throw new InvalidOperationException("You do not control a player in this game");

        // do not invite someone already in game
        if (state.JoinedPlayers.Any(p => p.OwnerId == command.InviteeId))
            throw new InvalidOperationException("Player already in game");

        yield return new PlayerInvited(state.GameId, command.OwnerId, command.InviteeId, DateTimeOffset.UtcNow);
    }

    // Minimal snapshot of game state needed to decide about inviting
    [BoundaryAggregate]
    public class PatiralGameState
    {
        public class PlayerState(string id, string ownerId)
        {
            public string Id { get; } = id;
            public string OwnerId { get; } = ownerId;
        }

        public List<PlayerState> JoinedPlayers { get; private set; } = new List<PlayerState>();
        public GameStatus GameStatus { get; private set; }
        public Guid GameId { get; private set; }

        public void Apply(object @event)
        {
            switch (@event)
            {
                case GameCreated gc:
                    GameId = gc.GameId;
                    GameStatus = GameStatus.Created;
                    break;
                case PlayerJoined pj:
                    JoinedPlayers.Add(new PlayerState(pj.PlayerId, pj.OwnerId));
                    break;
                case GameStarted:
                    GameStatus = GameStatus.Started;
                    break;
                case GameFinished:
                    GameStatus = GameStatus.Finished;
                    break;
                case GameDeleted:
                    GameStatus = GameStatus.Deleted;
                    break;
                    // ignore PlayerInvited for state since we don't persist invites in aggregate state
            }
        }
    }
}
