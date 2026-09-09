using Marten;
using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike;

public record TakeGemsCommand
{
    public Guid GameId { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string PlayerId { get; init; } = string.Empty;
    public int Diamond { get; init; }
    public int Sapphire { get; init; }
    public int Emerald { get; init; }
    public int Ruby { get; init; }
    public int Onyx { get; init; }
    public int Gold { get; init; }
}

public class TakeGemsCommandHandler
{
    private readonly IDocumentSession _session;

    public TakeGemsCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task Handle(TakeGemsCommand command, CancellationToken cancellationToken)
    {
        var query = TakeGemsDecisionState.Query(command.GameId);
        var boundary = await _session.Events.FetchForWritingByTags<TakeGemsDecisionState>(query, cancellationToken);
        var state = boundary.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state);

        boundary.AppendMany(events.Select(e => _session.Tag(e)).ToArray());
        await _session.SaveChangesAsync(cancellationToken);
    }

    internal static IReadOnlyList<IDomainEvent> Decide(TakeGemsCommand command, TakeGemsDecisionState state)
    {
        var gems = new GemCollection(
            command.Diamond,
            command.Sapphire,
            command.Emerald,
            command.Ruby,
            command.Onyx,
            command.Gold);

        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game deleted.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game finished.");
        if (!state.Started) throw new InvalidOperationException("Game not started.");
        if (!state.Players.TryGetValue(command.PlayerId, out var player)) throw new InvalidOperationException("Player not found.");
        if (player.OwnerId != command.OwnerId) throw new InvalidOperationException("You do not control this player.");
        if (state.CurrentPlayerId != command.PlayerId) throw new InvalidOperationException("Not your turn.");
        if (state.PendingGemReturnPlayerId is not null) throw new InvalidOperationException("A gem overflow resolution is pending.");

        ValidateSelection(gems);
        EnsureAvailable(state.MarketGems, gems);

        var now = DateTimeOffset.UtcNow;
        var events = new List<IDomainEvent>
        {
            new GemsTaken(command.GameId, command.PlayerId, gems, now)
        };

        var newTotal = player.Gems + gems;
        if (newTotal.Total > 10)
        {
            events.Add(new GemsOverflowDetected(command.GameId, command.PlayerId, newTotal, newTotal.Total - 10, now));
            return events;
        }

        var completionState = TurnCompletionDecisionState.From(state);
        completionState.Apply(events);
        events.AddRange(TurnCompletion.Decide(command.GameId, command.PlayerId, completionState, now));
        return events;
    }

    private static void ValidateSelection(GemCollection gems)
    {
        var colorCounts = new[] { gems.Diamond, gems.Sapphire, gems.Emerald, gems.Ruby, gems.Onyx };
        var nonZeroColors = colorCounts.Where(c => c > 0).ToList();

        var isOptionA = gems.Gold == 0 && nonZeroColors.All(c => c == 1) && nonZeroColors.Count is >= 1 and <= 3;
        var isOptionB = gems.Gold == 0 && nonZeroColors.Count == 1 && nonZeroColors[0] == 2;
        var isOptionC = gems.Gold == 1 && nonZeroColors.Count == 0;

        if (!isOptionA && !isOptionB && !isOptionC)
        {
            throw new InvalidOperationException("Invalid gem selection.");
        }
    }

    private static void EnsureAvailable(GemCollection marketGems, GemCollection gems)
    {
        if (gems.Diamond == 2 && marketGems.Diamond < 4) throw new InvalidOperationException("Not enough diamonds on market.");
        if (gems.Sapphire == 2 && marketGems.Sapphire < 4) throw new InvalidOperationException("Not enough sapphires on market.");
        if (gems.Emerald == 2 && marketGems.Emerald < 4) throw new InvalidOperationException("Not enough emeralds on market.");
        if (gems.Ruby == 2 && marketGems.Ruby < 4) throw new InvalidOperationException("Not enough rubies on market.");
        if (gems.Onyx == 2 && marketGems.Onyx < 4) throw new InvalidOperationException("Not enough onyxes on market.");

        if (marketGems.Diamond < gems.Diamond) throw new InvalidOperationException("Not enough diamonds on market.");
        if (marketGems.Sapphire < gems.Sapphire) throw new InvalidOperationException("Not enough sapphires on market.");
        if (marketGems.Emerald < gems.Emerald) throw new InvalidOperationException("Not enough emeralds on market.");
        if (marketGems.Ruby < gems.Ruby) throw new InvalidOperationException("Not enough rubies on market.");
        if (marketGems.Onyx < gems.Onyx) throw new InvalidOperationException("Not enough onyxes on market.");
        if (marketGems.Gold < gems.Gold) throw new InvalidOperationException("Not enough gold on market.");
    }
}
