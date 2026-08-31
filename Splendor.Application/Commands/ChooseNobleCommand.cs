using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record ChooseNobleCommand : IAuthoredCommand, IRequest
{
    public Guid GameId { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string PlayerId { get; init; } = string.Empty;
    public string NobleId { get; init; } = string.Empty;
}

public class ChooseNobleCommandHandler : IRequestHandler<ChooseNobleCommand>
{
    private readonly IEventStore _eventStore;

    public ChooseNobleCommandHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task Handle(ChooseNobleCommand request, CancellationToken cancellationToken)
    {
        var history = await _eventStore.FetchStreamAsync(request.GameId, cancellationToken);
        if (history.Count == 0) throw new InvalidOperationException("Game not found");

        var state = NobleSelectionState.From(history);
        var events = state.ChooseNoble(request.OwnerId, request.PlayerId, request.NobleId).ToList();

        await _eventStore.AppendAsync(request.GameId, events, cancellationToken);
        await _eventStore.SaveChangesAsync(cancellationToken);
    }

    private class NobleSelectionState
    {
        private readonly List<PlayerState> _players = new();
        private readonly HashSet<string> _availableNobleIds = new();
        private string _status = "Created";
        private string? _currentPlayerId;
        private PendingSelection? _pendingSelection;

        public static NobleSelectionState From(IEnumerable<object> history)
        {
            var state = new NobleSelectionState();
            foreach (var @event in history)
            {
                state.Apply(@event);
            }

            return state;
        }

        public IEnumerable<IDomainEvent> ChooseNoble(string ownerId, string playerId, string nobleId)
        {
            if (_status != "Started") throw new InvalidOperationException("Game is not active");
            if (_currentPlayerId != playerId) throw new InvalidOperationException("Not your turn");

            var player = _players.SingleOrDefault(player => player.Id == playerId)
                ?? throw new InvalidOperationException("Player not found");
            if (player.OwnerId != ownerId) throw new InvalidOperationException("You do not control this player");

            if (_pendingSelection is null || _pendingSelection.PlayerId != playerId)
                throw new InvalidOperationException("No noble selection is pending for this player");
            if (!_pendingSelection.EligibleNobleIds.Contains(nobleId))
                throw new InvalidOperationException("Selected noble is not eligible");
            if (!_availableNobleIds.Contains(nobleId))
                throw new InvalidOperationException("Noble is no longer available");

            var noble = NobleDefinitions.GetById(nobleId)
                ?? throw new InvalidOperationException("Noble not found");
            if (!MeetsRequirements(player, noble.Requirements))
                throw new InvalidOperationException("Player no longer meets this noble's requirements");

            var timestamp = DateTimeOffset.UtcNow;
            yield return new NobleAcquired(_pendingSelection.GameId, playerId, nobleId, timestamp);

            var totalPoints = GetPrestigePoints(player) + noble.PrestigePoints;
            if (totalPoints >= 15)
            {
                yield return new GameFinished(_pendingSelection.GameId, playerId, player.Name, totalPoints, timestamp);
                yield break;
            }

            yield return new TurnEnded(_pendingSelection.GameId, playerId, timestamp);
            yield return new TurnStarted(_pendingSelection.GameId, GetNextPlayer(playerId), timestamp);
        }

        private void Apply(object @event)
        {
            switch (@event)
            {
                case GameStarted gameStarted:
                    _status = "Started";
                    _availableNobleIds.Clear();
                    foreach (var nobleId in gameStarted.Nobles) _availableNobleIds.Add(nobleId);
                    break;
                case PlayerJoined playerJoined:
                    _players.Add(new PlayerState(playerJoined.PlayerId, playerJoined.OwnerId, playerJoined.Name));
                    break;
                case TurnStarted turnStarted:
                    _currentPlayerId = turnStarted.PlayerId;
                    break;
                case CardPurchased cardPurchased:
                    _players.SingleOrDefault(player => player.Id == cardPurchased.PlayerId)?.OwnedCardIds.Add(cardPurchased.CardId);
                    break;
                case NobleSelectionRequired selectionRequired:
                    _pendingSelection = new PendingSelection(selectionRequired.GameId, selectionRequired.PlayerId, selectionRequired.EligibleNobleIds);
                    break;
                case NobleAcquired nobleAcquired:
                    _players.SingleOrDefault(player => player.Id == nobleAcquired.PlayerId)?.OwnedNobleIds.Add(nobleAcquired.NobleId);
                    _availableNobleIds.Remove(nobleAcquired.NobleId);
                    _pendingSelection = null;
                    break;
                case GameFinished:
                    _status = "Finished";
                    _currentPlayerId = null;
                    break;
                case GameDeleted:
                    _status = "Deleted";
                    _currentPlayerId = null;
                    break;
            }
        }

        private string GetNextPlayer(string playerId)
        {
            var playerIndex = _players.FindIndex(player => player.Id == playerId);
            return _players[(playerIndex + 1) % _players.Count].Id;
        }

        private static bool MeetsRequirements(PlayerState player, GemCollection requirements)
        {
            var bonuses = GetBonuses(player);
            return bonuses.Diamond >= requirements.Diamond &&
                bonuses.Sapphire >= requirements.Sapphire &&
                bonuses.Emerald >= requirements.Emerald &&
                bonuses.Ruby >= requirements.Ruby &&
                bonuses.Onyx >= requirements.Onyx;
        }

        private static GemCollection GetBonuses(PlayerState player)
        {
            var bonuses = new int[5];
            foreach (var cardId in player.OwnedCardIds)
            {
                switch (CardDefinitions.GetById(cardId)?.BonusType)
                {
                    case GemType.Diamond: bonuses[0]++; break;
                    case GemType.Sapphire: bonuses[1]++; break;
                    case GemType.Emerald: bonuses[2]++; break;
                    case GemType.Ruby: bonuses[3]++; break;
                    case GemType.Onyx: bonuses[4]++; break;
                }
            }

            return new GemCollection(bonuses[0], bonuses[1], bonuses[2], bonuses[3], bonuses[4], 0);
        }

        private static int GetPrestigePoints(PlayerState player) =>
            player.OwnedCardIds.Sum(cardId => CardDefinitions.GetById(cardId)?.PrestigePoints ?? 0) +
            player.OwnedNobleIds.Sum(nobleId => NobleDefinitions.GetById(nobleId)?.PrestigePoints ?? 0);

        private class PlayerState(string id, string ownerId, string name)
        {
            public string Id { get; } = id;
            public string OwnerId { get; } = ownerId;
            public string Name { get; } = name;
            public List<string> OwnedCardIds { get; } = new();
            public List<string> OwnedNobleIds { get; } = new();
        }

        private class PendingSelection
        {
            public Guid GameId { get; }
            public string PlayerId { get; }
            public HashSet<string> EligibleNobleIds { get; }

            public PendingSelection(Guid gameId, string playerId, IEnumerable<string> eligibleNobleIds)
            {
                GameId = gameId;
                PlayerId = playerId;
                EligibleNobleIds = new HashSet<string>(eligibleNobleIds);
            }
        }
    }
}
