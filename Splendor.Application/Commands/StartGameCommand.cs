using Marten;
using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.DecisionStates;
using Splendor.Application.Events;
using Splendor.Application.Snapshots;
using Splendor.Domain;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.Rules;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record StartGameCommand : IAuthorizedCommand, IRequest
{
    public Guid GameId { get; init; }
    public Caller Caller { get; init; }

    public StartGameCommand(Guid gameId, Caller caller)
    {
        GameId = gameId;
        Caller = caller;
    }
}

public class StartGameCommandHandler : IRequestHandler<StartGameCommand>
{
    private readonly IDocumentSession _session;
    private readonly TimeProvider _timeProvider;

    public StartGameCommandHandler(IDocumentSession session, TimeProvider timeProvider)
    {
        _session = session;
        _timeProvider = timeProvider;
    }

    public async Task Handle(StartGameCommand command, CancellationToken cancellationToken)
    {
        var stream = await _session.Events.FetchForWriting<SplendorGameState>(command.GameId, cancellationToken);
        var state = stream.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state, _timeProvider.GetUtcNow());

        stream.AppendMany(events.Select(e => _session.TagEvent(e)));
        await _session.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<IDomainEvent> Decide(StartGameCommand command, SplendorGameState state, DateTimeOffset now)
    {
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game deleted.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game finished.");
        if (state.Status == GameStatus.Started) throw new InvalidOperationException("Game already started.");
        if (state.Status != GameStatus.Created) throw new InvalidOperationException("Game not created.");
        if (state.PlayerOrder.Count < 2) throw new InvalidOperationException("Need at least 2 players.");
        if (state.PlayerOrder.Count > 4) throw new InvalidOperationException("Too many players.");

        var isCreator = state.CreatorId == command.Caller.UserId.Value;
        var isParticipant = state.Players.Values.Any(p => p.OwnerId == command.Caller.UserId.Value);

        if (!isCreator && !isParticipant)
        {
            throw new InvalidOperationException("Only the creator or a participant can start the game.");
        }

        // Shuffle and setup card decks
        var random = new Random();
        var deck1 = CardDefinitions.GetLevel(1).Select(c => c.Id).OrderBy(_ => random.Next()).ToList();
        var deck2 = CardDefinitions.GetLevel(2).Select(c => c.Id).OrderBy(_ => random.Next()).ToList();
        var deck3 = CardDefinitions.GetLevel(3).Select(c => c.Id).OrderBy(_ => random.Next()).ToList();

        // Draw 4 cards for each market
        var market1 = deck1.Take(4).ToList();
        deck1 = deck1.Skip(4).ToList();
        var market2 = deck2.Take(4).ToList();
        deck2 = deck2.Skip(4).ToList();
        var market3 = deck3.Take(4).ToList();
        deck3 = deck3.Skip(4).ToList();

        // Pick 3 random nobles for this game
        var nobleIds = new List<string>();
        var allNobles = NobleDefinitions.AllNobles.Select(n => n.Id).ToList();
        // shuffle nobles
        allNobles = allNobles.OrderBy(_ => random.Next()).ToList();
        nobleIds = allNobles.Take(Math.Min(3, allNobles.Count)).ToList();

        return new List<IDomainEvent>
        {
            new GameStarted(
                command.GameId,
                SplendorRules.StartingMarketGems(state.PlayerOrder.Count),
                deck1,
                deck2,
                deck3,
                market1,
                market2,
                market3,
                nobleIds,
                now),
            new TurnStarted(command.GameId, state.PlayerOrder[0], now),
            TurnClockState.Start(command.GameId, state.PlayerOrder[0], now)
        };
    }
}
