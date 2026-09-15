using Marten;
using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.DecisionStates;
using Splendor.Application.Events;
using Splendor.Domain;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.Rules;
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
    private readonly IDocumentSession _session;

    public ChooseNobleCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task Handle(ChooseNobleCommand command, CancellationToken cancellationToken)
    {
        var query = SplendorGameState.Query(command.GameId);
        var boundary = await _session.Events.FetchForWritingByTags<SplendorGameState>(query, cancellationToken);
        var state = boundary.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state).ToList();

        state.Apply(events);

        var completionEvents = TurnCompletion.Decide(command.GameId, command.PlayerId, state, DateTimeOffset.UtcNow);
        events.AddRange(completionEvents);

        boundary.AppendMany(events.Select(e => _session.TagEvent(e)).ToArray());
        await _session.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<IDomainEvent> Decide(ChooseNobleCommand command, SplendorGameState state)
    {
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game deleted.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game finished.");
        if (state.Status != GameStatus.Started) throw new InvalidOperationException("Game is not active.");
        if (state.CurrentPlayerId != command.PlayerId) throw new InvalidOperationException("Not your turn.");
        if (!state.Players.TryGetValue(command.PlayerId, out var player)) throw new InvalidOperationException("Player not found.");
        if (player.OwnerId != command.OwnerId) throw new InvalidOperationException("You do not control this player.");
        if (state.PendingNobleSelectionPlayerId != command.PlayerId)
            throw new InvalidOperationException("No noble selection is pending for this player.");
        if (!state.EligibleNobleIds.Contains(command.NobleId))
            throw new InvalidOperationException("Selected noble is not eligible.");
        if (!state.Nobles.Contains(command.NobleId))
            throw new InvalidOperationException("Noble is no longer available.");

        var noble = NobleDefinitions.GetById(command.NobleId) ?? throw new InvalidOperationException("Noble not found.");
        var bonuses = SplendorRules.GetBonuses(player.OwnedCardIds);
        if (!SplendorRules.MeetsNobleRequirements(bonuses, noble.Requirements))
        {
            throw new InvalidOperationException("Player no longer meets this noble's requirements.");
        }

        var now = DateTimeOffset.UtcNow;
        return new List<IDomainEvent>
        {
            new NobleAcquired(command.GameId, command.PlayerId, command.NobleId, now)
        };
    }
}
