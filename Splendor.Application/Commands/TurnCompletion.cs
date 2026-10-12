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
    public static IEnumerable<IDomainEvent> DecideAfterAction(SplendorGameState state, TurnClockState clock, DateTimeOffset now)
    {
        var playerId = CurrentPlayerId(state);

        // The overflow was already detected by the action; the player gets a fresh deadline to return gems.
        if (state.PendingGemReturnPlayerId == playerId)
            return new List<IDomainEvent> { CreateTurnDeadlineStarted(state, clock, playerId, now) };
        if (state.PendingGemReturnPlayerId is not null) throw new InvalidOperationException("A gem overflow resolution is pending.");
        if (state.PendingNobleSelectionPlayerId == playerId) throw new InvalidOperationException("A noble selection is pending.");

        if (!state.Players.TryGetValue(playerId, out var player)) throw new InvalidOperationException("Player not found.");

        if (player.Gems.Total > SplendorRules.MaxPlayerGems)
        {
            return new List<IDomainEvent>
            {
                new GemsOverflowDetected(state.Id, playerId, player.Gems, player.Gems.Total - SplendorRules.MaxPlayerGems, now),
                CreateTurnDeadlineStarted(state, clock, playerId, now)
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
                new NobleSelectionRequired(state.Id, playerId, eligible.Select(noble => noble.Id).ToList(), now),
                CreateTurnDeadlineStarted(state, clock, playerId, now)
            };
        }

        var events = new List<IDomainEvent>();
        var acquired = eligible.Count == 1 ? eligible.Single() : null;
        if (acquired != null)
        {
            events.Add(new NobleAcquired(state.Id, playerId, acquired.Id, now));
        }

        var pointsFromAcquiredNoble = acquired?.PrestigePoints ?? 0;
        events.AddRange(DecideTurnEnd(playerId, state, clock, pointsFromAcquiredNoble, now));
        return events;
    }

    public static IEnumerable<IDomainEvent> DecideAfterNobleSelection(SplendorGameState state, TurnClockState clock, DateTimeOffset now) =>
        DecideTurnEnd(CurrentPlayerId(state), state, clock, 0, now);

    public static IEnumerable<IDomainEvent> DecideAfterExpiration(SplendorGameState state, TurnClockState clock, DateTimeOffset now) =>
        DecideTurnEnd(CurrentPlayerId(state), state, clock, 0, now);

    private static IEnumerable<IDomainEvent> DecideTurnEnd(string playerId, SplendorGameState state, TurnClockState clock, int pointsFromAcquiredNoble, DateTimeOffset now)
    {
        if (!state.Players.TryGetValue(playerId, out var player)) throw new InvalidOperationException("Player not found.");

        var events = new List<IDomainEvent>();
        var nextPlayerId = state.NextPlayerAfter(playerId);
        var isRoundComplete = nextPlayerId == state.PlayerOrder[0];
        var playerPoints = SplendorRules.GetPrestigePoints(player.OwnedCardIds, player.OwnedNobleIds) + pointsFromAcquiredNoble;
        var hasEndGameStarted = playerPoints >= SplendorRules.EndGamePrestigePoints || state.Players.Values.Any(p => SplendorRules.GetPrestigePoints(p.OwnedCardIds, p.OwnedNobleIds) >= SplendorRules.EndGamePrestigePoints);

        events.Add(new TurnEnded(state.Id, playerId, now));

        if (hasEndGameStarted && isRoundComplete)
        {
            var winner = SelectWinner(state, playerId, pointsFromAcquiredNoble);
            events.Add(new GameFinished(state.Id, winner.PlayerId, winner.OwnerId, winner.Name, winner.PrestigePoints, now));
            events.AddRange(state.Players.Select(candidate =>
                (IDomainEvent)new PlayerParticipationEnded(state.Id, candidate.Key, candidate.Value.PlayerOwnerId, now)));
            return events;
        }

        events.Add(new TurnStarted(state.Id, nextPlayerId, now));
        events.Add(CreateTurnDeadlineStarted(state, clock, nextPlayerId, now));
        return events;
    }

    private static string CurrentPlayerId(SplendorGameState state) =>
        state.CurrentPlayerId ?? throw new InvalidOperationException("No current player.");

    private static TurnDeadlineStarted CreateTurnDeadlineStarted(SplendorGameState state, TurnClockState clock, string playerId, DateTimeOffset now) =>
        new(state.Id, Guid.NewGuid(), playerId, now.AddSeconds(clock.DurationSeconds), now);

    private static PlayerScoreCandidate SelectWinner(SplendorGameState state, string currentPlayerId, int currentPlayerExtraPoints)
    {
        var candidates = state.Players.Select(player =>
        {
            var extraPoints = player.Key == currentPlayerId ? currentPlayerExtraPoints : 0;

            return new PlayerScoreCandidate(
                PlayerId: player.Key,
                OwnerId: player.Value.PlayerOwnerId,
                Name: player.Value.Name,
                PrestigePoints: SplendorRules.GetPrestigePoints(player.Value.OwnedCardIds, player.Value.OwnedNobleIds) + extraPoints,
                PurchasedCardCount: player.Value.OwnedCardIds.Count,
                PlayerOrder: state.PlayerOrder.IndexOf(player.Key));
        });

        return SplendorRules.SelectWinner(candidates);
    }
}
