using Splendor.Domain.Rules;
using Marten;
using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.Events;
using Splendor.Application.Snapshots;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record ResolveGemLimitCommand : IAuthorizedCommand, IRequest
{
    public Guid GameId { get; init; }
    public required Caller Caller { get; init; }
    public string PlayerId { get; init; } = string.Empty;
    public int Diamond { get; init; }
    public int Sapphire { get; init; }
    public int Emerald { get; init; }
    public int Ruby { get; init; }
    public int Onyx { get; init; }
    public int Gold { get; init; }
}

public class ResolveGemLimitCommandHandler : IRequestHandler<ResolveGemLimitCommand>
{
    private readonly IDocumentSession _session;

    public ResolveGemLimitCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task Handle(ResolveGemLimitCommand command, CancellationToken cancellationToken)
    {
        var stream = await _session.Events.FetchForWriting<SplendorGameState>(command.GameId, cancellationToken);
        var state = stream.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state).ToList();

        // Apply decision events to local state
        state.Apply(events);

        // Turn completion may produce additional events; merge them
        var completionEvents = TurnCompletion.DecideAfterAction(command.GameId, command.PlayerId, state, DateTimeOffset.UtcNow);
        events.AddRange(completionEvents);

        // Tag and append events to the stream
        stream.AppendMany(events.Select(e => _session.TagEvent(e)));
        await _session.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<IDomainEvent> Decide(ResolveGemLimitCommand command, SplendorGameState state)
    {
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game deleted.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game finished.");
        if (state.Status != GameStatus.Started) throw new InvalidOperationException("Game not started.");
        if (!state.Players.TryGetValue(command.PlayerId, out var player)) throw new InvalidOperationException("Player not found.");
        if (player.OwnerId != command.Caller.UserId.Value) throw new InvalidOperationException("You do not control this player.");
        if (state.PendingGemReturnPlayerId != command.PlayerId) throw new InvalidOperationException("No gem return is required for this player.");

        var returnedGems = new GemCollection(command.Diamond, command.Sapphire, command.Emerald, command.Ruby, command.Onyx, command.Gold);
        if (returnedGems.Diamond > player.Gems.Diamond) throw new InvalidOperationException("Cannot return more diamonds than owned.");
        if (returnedGems.Sapphire > player.Gems.Sapphire) throw new InvalidOperationException("Cannot return more sapphires than owned.");
        if (returnedGems.Emerald > player.Gems.Emerald) throw new InvalidOperationException("Cannot return more emeralds than owned.");
        if (returnedGems.Ruby > player.Gems.Ruby) throw new InvalidOperationException("Cannot return more rubies than owned.");
        if (returnedGems.Onyx > player.Gems.Onyx) throw new InvalidOperationException("Cannot return more onyxes than owned.");
        if (returnedGems.Gold > player.Gems.Gold) throw new InvalidOperationException("Cannot return more gold than owned.");
        if ((player.Gems - returnedGems).Total > 10) throw new InvalidOperationException("Returned gems do not reduce total to allowed limit.");

        return new List<IDomainEvent>
        {
            new GemLimitResolved(command.GameId, command.PlayerId, returnedGems, DateTimeOffset.UtcNow)
        };
    }
}
