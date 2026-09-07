using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain.Common;
using Splendor.Domain.Events;

namespace Splendor.Application.Commands;

public record InvitePlayerCommand : IAuthoredCommand, IRequest
{
    public Guid GameId { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string InviteeId { get; init; } = string.Empty;
}

public class InvitePlayerCommandHandler : IRequestHandler<InvitePlayerCommand>
{
    private readonly IEventStore _eventStore;

    public InvitePlayerCommandHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task Handle(InvitePlayerCommand request, CancellationToken cancellationToken)
    {
        var history = await _eventStore.FetchStreamAsync(request.GameId, cancellationToken);
        if (history.Count == 0) throw new InvalidOperationException("Game not found");

        var state = PatiralGameState.From(history);
        var events = state.InvitePlayer(request.OwnerId, request.InviteeId).ToList();

        await _eventStore.AppendAsync(request.GameId, events, cancellationToken);
        await _eventStore.SaveChangesAsync(cancellationToken);
    }

    // Minimal snapshot of game state needed to decide about inviting
    private class PatiralGameState
    {
        private class PlayerState(string id, string ownerId)
        {
            public string Id { get; } = id;
            public string OwnerId { get; } = ownerId;
        }

        private readonly List<PlayerState> _joinedPlayers = new();
        private string _gameStatus = "Created";
        private Guid _gameId = Guid.Empty;

        public static PatiralGameState From(IEnumerable<object> history)
        {
            var state = new PatiralGameState();
            foreach (var @event in history)
            {
                state.Apply(@event);
            }
            return state;
        }

        public IEnumerable<IDomainEvent> InvitePlayer(string ownerId, string inviteeId)
        {
            if (_gameId == Guid.Empty) throw new InvalidOperationException("GameId missing in history");

            // mirror Game.EnsureActive(): cannot invite after start/finish/delete
            if (_gameStatus == "Started") throw new InvalidOperationException("Game is already started.");
            if (_gameStatus == "Finished") throw new InvalidOperationException("Game is already finished.");
            if (_gameStatus == "Deleted") throw new InvalidOperationException("Game has been deleted.");

            // inviter must control a player in this game
            if (!_joinedPlayers.Any(p => p.OwnerId == ownerId))
                throw new InvalidOperationException("You do not control a player in this game");

            // do not invite someone already in game
            if (_joinedPlayers.Any(p => p.OwnerId == inviteeId))
                throw new InvalidOperationException("Player already in game");

            yield return new PlayerInvited(_gameId, ownerId, inviteeId, DateTimeOffset.UtcNow);
        }

        private void Apply(object @event)
        {
            switch (@event)
            {
                case GameCreated gc:
                    _gameId = gc.GameId;
                    _gameStatus = "Created";
                    break;
                case PlayerJoined pj:
                    _joinedPlayers.Add(new PlayerState(pj.PlayerId, pj.OwnerId));
                    break;
                case GameStarted:
                    _gameStatus = "Started";
                    break;
                case GameFinished:
                    _gameStatus = "Finished";
                    break;
                case GameDeleted:
                    _gameStatus = "Deleted";
                    break;
                    // ignore PlayerInvited for state since we don't persist invites in aggregate state
            }
        }
    }
}