using MediatR;
using Marten;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.DecisionStates;
using Splendor.Application.Events;
using Splendor.Domain.Events;
using Splendor.Domain.Rules;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record CreateGameCommand : IAuthorizedCommand, IRequest<Guid>
{
    public required Caller Caller { get; init; }
    public GameName? Name { get; init; }
    public int TurnDurationSeconds { get; init; } = PlatformRules.DefaultTurnSeconds;
}

public class CreateGameCommandHandler : IRequestHandler<CreateGameCommand, Guid>
{
    private readonly IDocumentSession _session;

    public CreateGameCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task<Guid> Handle(CreateGameCommand command, CancellationToken cancellationToken)
    {
        var creatorId = command.Caller.UserId.Value;
        var boundary = await _session.Events.FetchForWritingByTags<CreateGameDecisionState>(
            CreateGameDecisionState.Query(creatorId), cancellationToken);
        var openGames = boundary.Aggregate?.OpenGameIds.Count ?? 0;
        if (!PlatformRules.CanCreateGame(openGames))
            throw new InvalidOperationException($"You cannot have more than {PlatformRules.MaxOpenCreatedGames} unfinished games.");

        if (!PlatformRules.IsValidTurnDuration(command.TurnDurationSeconds))
            throw new InvalidOperationException(
                $"Turn duration must be {PlatformRules.MinTurnSeconds}-{PlatformRules.MaxTurnSeconds} seconds.");

        var gameId = Guid.NewGuid();
        var @event = new GameCreated(gameId, creatorId, DateTimeOffset.UtcNow, command.Name?.Value, command.TurnDurationSeconds);

        _session.Events.StartStream(gameId, _session.TagEvent(@event));
        await _session.SaveChangesAsync(cancellationToken);

        return gameId;
    }
}
