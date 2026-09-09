using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike;

internal static class TurnCompletion
{
    public static IEnumerable<IDomainEvent> Decide(Guid gameId, string playerId, TurnCompletionDecisionState state, DateTimeOffset now)
    {
        if (!state.Players.TryGetValue(playerId, out var player)) throw new InvalidOperationException("Player not found.");

        var bonuses = GetPlayerBonuses(player);
        var eligible = state.Nobles
            .Select(NobleDefinitions.GetById)
            .Where(noble => noble != null && MeetsNobleRequirements(bonuses, noble.Requirements))
            .Cast<Noble>()
            .ToList();

        if (eligible.Count > 1)
        {
            return [new NobleSelectionRequired(gameId, playerId, eligible.Select(noble => noble.Id).ToList(), now)];
        }

        var events = new List<IDomainEvent>();
        var acquired = eligible.Count == 1 ? eligible.Single() : null;
        if (acquired != null)
        {
            events.Add(new NobleAcquired(gameId, playerId, acquired.Id, now));

            var totalPoints = GetPlayerPrestigePoints(player) + acquired.PrestigePoints;
            if (totalPoints >= 15)
            {
                events.Add(new GameFinished(gameId, playerId, player.OwnerId, player.Name, totalPoints, now));
                return events;
            }
        }

        events.Add(new TurnEnded(gameId, playerId, now));
        events.Add(new TurnStarted(gameId, state.NextPlayerAfter(playerId), now));
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
            switch (CardCatalog.GetById(cardId)?.BonusType)
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
        player.OwnedCardIds
            .Sum(cardId => CardCatalog.GetById(cardId)?.PrestigePoints ?? 0);

    private static bool MeetsNobleRequirements(GemCollection bonuses, GemCollection requirements) =>
        bonuses.Diamond >= requirements.Diamond &&
        bonuses.Sapphire >= requirements.Sapphire &&
        bonuses.Emerald >= requirements.Emerald &&
        bonuses.Ruby >= requirements.Ruby &&
        bonuses.Onyx >= requirements.Onyx;
}
