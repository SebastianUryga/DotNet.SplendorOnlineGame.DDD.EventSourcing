using Marten;
using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;
using Splendor.Application.Events;
using Splendor.Application.DecisionStates;
using Splendor.Domain.Rules;

namespace Splendor.Application.Commands;

public record TakeGemsCommand : IAuthoredCommand, IRequest
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

public class TakeGemsCommandHandler : IRequestHandler<TakeGemsCommand>
{
    private readonly IDocumentSession _session;

    public TakeGemsCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task Handle(TakeGemsCommand command, CancellationToken cancellationToken)
    {
        var query = SplendorGameState.Query(command.GameId);
        var boundary = await _session.Events.FetchForWritingByTags<SplendorGameState>(query, cancellationToken);
        var state = boundary.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state).ToList();

        // Apply decision events to local state
        state.Apply(events);

        // Turn completion may produce additional events; merge them
        var completionEvents = TurnCompletion.Decide(command.GameId, command.PlayerId, state, DateTimeOffset.UtcNow);
        events.AddRange(completionEvents);

        // Tag and append events to the boundary
        boundary.AppendMany(events.Select(e => _session.TagEvent(e)).ToArray());
        await _session.SaveChangesAsync(cancellationToken);
    }

    internal static IReadOnlyList<IDomainEvent> Decide(TakeGemsCommand command, SplendorGameState state)
    {
        var gems = new GemCollection(command.Diamond, command.Sapphire, command.Emerald, command.Ruby, command.Onyx, command.Gold);

        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game deleted.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game finished.");
        if (state.Status != GameStatus.Started) throw new InvalidOperationException("Game not started.");
        if (!state.Players.TryGetValue(command.PlayerId, out var player)) throw new InvalidOperationException("Player not found.");
        if (player.OwnerId != command.OwnerId) throw new InvalidOperationException("You do not control this player.");
        if (state.CurrentPlayerId != command.PlayerId) throw new InvalidOperationException("Not your turn.");
        if (state.PendingGemReturnPlayerId is not null) throw new InvalidOperationException("A gem overflow resolution is pending.");
        if (state.PendingNobleSelectionPlayerId is not null) throw new InvalidOperationException("A noble selection is pending.");

        SplendorRules.EnsureValidGemSelection(gems);
        SplendorRules.EnsureGemsAvailable(state.MarketGems, gems);

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

        return events;
    }
}
