using Splendor.Application.DecisionStates;
using Splendor.Domain;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

internal static class TurnCompletion
{
    public static IEnumerable<IDomainEvent> Decide(Guid gameId, string playerId, SplendorGameState state, DateTimeOffset now)
    {
        if (state.PendingGemReturnPlayerId == playerId) return Enumerable.Empty<IDomainEvent>();
        if (state.PendingGemReturnPlayerId is not null) throw new InvalidOperationException("A gem overflow resolution is pending.");
        if (state.PendingNobleSelectionPlayerId == playerId) throw new InvalidOperationException("A noble selection is pending.");

        if (!state.Players.TryGetValue(playerId, out var player)) throw new InvalidOperationException("Player not found.");

        if (player.Gems.Total > 10)
        {
            return new List<IDomainEvent>
            {
                new GemsOverflowDetected(gameId, playerId, player.Gems, player.Gems.Total - 10, now)
            };
        }

        var bonuses = GetPlayerBonuses(player);
        var eligible = state.Nobles
            .Select(NobleDefinitions.GetById)
            .Where(noble => noble != null && MeetsNobleRequirements(bonuses, noble.Requirements))
            .Cast<Noble>()
            .ToList();

        if (eligible.Count > 1)
        {
            return new List<IDomainEvent>
            {
                new NobleSelectionRequired(gameId, playerId, eligible.Select(noble => noble.Id).ToList(), now)
            };
        }

        var events = new List<IDomainEvent>();
        var acquired = eligible.Count == 1 ? eligible.Single() : null;
        if (acquired != null)
        {
            events.Add(new NobleAcquired(gameId, playerId, acquired.Id, now));
        }

        var nextPlayerId = state.NextPlayerAfter(playerId);
        var isRoundComplete = nextPlayerId == state.PlayerOrder[0];
        var pointsFromAcquiredNoble = acquired?.PrestigePoints ?? 0;
        var playerPoints = GetPlayerPrestigePoints(player) + pointsFromAcquiredNoble;
        var hasEndGameStarted = playerPoints >= 15 || state.Players.Values.Any(p => GetPlayerPrestigePoints(p) >= 15);

        events.Add(new TurnEnded(gameId, playerId, now));

        if (hasEndGameStarted && isRoundComplete)
        {
            var winner = SelectWinner(state, playerId, pointsFromAcquiredNoble);
            events.Add(new GameFinished(gameId, winner.PlayerId, winner.Player.OwnerId, winner.Player.Name, winner.Points, now));
            return events;
        }

        events.Add(new TurnStarted(gameId, nextPlayerId, now));
        return events;
    }

    private static GemCollection GetPlayerBonuses(PlayerState player)
    {
        var diamond = 0;
        var sapphire = 0;
        var emerald = 0;
        var ruby = 0;
        var onyx = 0;

        foreach (var cardId in player.OwnedCardIds)
        {
            switch (CardDefinitions.GetById(cardId)?.BonusType)
            {
                case GemType.Diamond: diamond++; break;
                case GemType.Sapphire: sapphire++; break;
                case GemType.Emerald: emerald++; break;
                case GemType.Ruby: ruby++; break;
                case GemType.Onyx: onyx++; break;
            }
        }

        return new GemCollection(diamond, sapphire, emerald, ruby, onyx, 0);
    }

    private static int GetPlayerPrestigePoints(PlayerState player) =>
        player.OwnedCardIds.Sum(cardId => CardDefinitions.GetById(cardId)?.PrestigePoints ?? 0) +
        player.OwnedNobleIds.Sum(nobleId => NobleDefinitions.GetById(nobleId)?.PrestigePoints ?? 0);

    private static (string PlayerId, PlayerState Player, int Points) SelectWinner(SplendorGameState state, string currentPlayerId, int currentPlayerExtraPoints) =>
        state.Players
            .Select(entry => (
                PlayerId: entry.Key,
                Player: entry.Value,
                Points: GetPlayerPrestigePoints(entry.Value) + (entry.Key == currentPlayerId ? currentPlayerExtraPoints : 0),
                PlayerOrder: state.PlayerOrder.IndexOf(entry.Key)))
            .OrderByDescending(player => player.Points)
            .ThenBy(player => player.Player.OwnedCardIds.Count)
            .ThenBy(player => player.PlayerOrder)
            .Select(player => (player.PlayerId, player.Player, player.Points))
            .First();

    private static bool MeetsNobleRequirements(GemCollection bonuses, GemCollection requirements) =>
        bonuses.Diamond >= requirements.Diamond &&
        bonuses.Sapphire >= requirements.Sapphire &&
        bonuses.Emerald >= requirements.Emerald &&
        bonuses.Ruby >= requirements.Ruby &&
        bonuses.Onyx >= requirements.Onyx;
}
