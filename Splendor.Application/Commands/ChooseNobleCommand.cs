using Marten;
using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.Events;
using Splendor.Application.Snapshots;
using Splendor.Domain;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.Rules;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record ChooseNobleCommand : IAuthorizedCommand, IRequest
{
    public Guid GameId { get; init; }
    public required Caller Caller { get; init; }
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
        var stream = await _session.Events.FetchForWriting<SplendorGameState>(command.GameId, cancellationToken);
        var state = stream.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state).ToList();

        // Apply decision events to local state
        state.Apply(events);

        // Turn completion may produce additional events; merge them
        var completionEvents = TurnCompletion.DecideAfterNobleSelection(command.GameId, command.PlayerId, state, DateTimeOffset.UtcNow);
        events.AddRange(completionEvents);

        // Tag and append events to the stream
        stream.AppendMany(events.Select(e => _session.TagEvent(e, state.CreatorId)));
        await _session.SaveChangesAsync(cancellationToken);
    }

    internal static IReadOnlyList<IDomainEvent> Decide(ChooseNobleCommand command, SplendorGameState state)
    {
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game deleted.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game finished.");
        if (state.Status != GameStatus.Started) throw new InvalidOperationException("Game is not active.");
        if (state.CurrentPlayerId != command.PlayerId) throw new InvalidOperationException("Not your turn.");
        if (!state.Players.TryGetValue(command.PlayerId, out var player)) throw new InvalidOperationException("Player not found.");
        if (player.OwnerId != command.Caller.UserId.Value) throw new InvalidOperationException("You do not control this player.");
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
