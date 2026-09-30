using Splendor.Application.Snapshots;
using Splendor.Application.DecisionStates;
using Splendor.Domain;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.Rules;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

internal static class TurnCompletion
{
    public static IEnumerable<IDomainEvent> DecideAfterAction(Guid gameId, string playerId, SplendorGameState state, DateTimeOffset now)
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

        var bonuses = SplendorRules.GetBonuses(player.OwnedCardIds);
        var eligible = state.Nobles
            .Select(NobleDefinitions.GetById)
            .Where(noble => noble != null && SplendorRules.MeetsNobleRequirements(bonuses, noble.Requirements))
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

        var pointsFromAcquiredNoble = acquired?.PrestigePoints ?? 0;
        events.AddRange(DecideTurnEnd(gameId, playerId, state, pointsFromAcquiredNoble, now));
        return events;
    }

    public static IEnumerable<IDomainEvent> DecideAfterNobleSelection(Guid gameId, string playerId, SplendorGameState state, DateTimeOffset now) =>
        DecideTurnEnd(gameId, playerId, state, 0, now);

    public static IEnumerable<IDomainEvent> DecideAfterExpiration(Guid gameId, string playerId, SplendorGameState state, DateTimeOffset now) =>
        DecideTurnEnd(gameId, playerId, state, 0, now);

    private static IEnumerable<IDomainEvent> DecideTurnEnd(Guid gameId, string playerId, SplendorGameState state, int pointsFromAcquiredNoble, DateTimeOffset now)
    {
        if (!state.Players.TryGetValue(playerId, out var player)) throw new InvalidOperationException("Player not found.");

        var events = new List<IDomainEvent>();
        var nextPlayerId = state.NextPlayerAfter(playerId);
        var isRoundComplete = nextPlayerId == state.PlayerOrder[0];
        var playerPoints = SplendorRules.GetPrestigePoints(player.OwnedCardIds, player.OwnedNobleIds) + pointsFromAcquiredNoble;
        var hasEndGameStarted = playerPoints >= 15 || state.Players.Values.Any(p => SplendorRules.GetPrestigePoints(p.OwnedCardIds, p.OwnedNobleIds) >= 15);

        events.Add(new TurnEnded(gameId, playerId, now));

        if (hasEndGameStarted && isRoundComplete)
        {
            var winner = SelectWinner(state, playerId, pointsFromAcquiredNoble);
            events.Add(new GameFinished(gameId, winner.PlayerId, winner.OwnerId, winner.Name, winner.PrestigePoints, now));
            events.AddRange(state.Players.Select(candidate =>
                (IDomainEvent)new PlayerParticipationEnded(gameId, candidate.Key, candidate.Value.OwnerId, now)));
            return events;
        }

        events.Add(new TurnStarted(gameId, nextPlayerId, now));
        events.Add(TurnClockState.Start(gameId, nextPlayerId, now));
        return events;
    }

    private static PlayerScoreCandidate SelectWinner(SplendorGameState state, string currentPlayerId, int currentPlayerExtraPoints)
    {
        var candidates = state.Players.Select(player =>
        {
            var extraPoints = player.Key == currentPlayerId ? currentPlayerExtraPoints : 0;

            return new PlayerScoreCandidate(
                PlayerId: player.Key,
                OwnerId: player.Value.OwnerId,
                Name: player.Value.Name,
                PrestigePoints: SplendorRules.GetPrestigePoints(player.Value.OwnedCardIds, player.Value.OwnedNobleIds) + extraPoints,
                PurchasedCardCount: player.Value.OwnedCardIds.Count,
                PlayerOrder: state.PlayerOrder.IndexOf(player.Key));
        });

        return SplendorRules.SelectWinner(candidates);
    }
}
