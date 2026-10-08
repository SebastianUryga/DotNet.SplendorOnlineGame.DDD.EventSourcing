using Splendor.Domain.Rules;
using MediatR;
using Marten;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.Events;
using Splendor.Domain.Events;
using Splendor.Application.Snapshots;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record CreateGameCommand : IAuthorizedCommand, IRequest<Guid>
{
    public required Caller Caller { get; init; }
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
        // ponytail: counts the async snapshot, so a burst of creates can slightly exceed the limit; use a DCB boundary if it must be strict.
        var userId = command.Caller.UserId.Value;
        var openGames = await _session.Query<SplendorGameState>().CountAsync(
            state => state.CreatorId == userId && (state.Status == GameStatus.Created || state.Status == GameStatus.Started),
            cancellationToken);
        if (!PlatformRules.CanCreateGame(openGames))
            throw new InvalidOperationException($"You cannot have more than {PlatformRules.MaxOpenCreatedGames} unfinished games.");

        var gameId = Guid.NewGuid();
        var @event = new GameCreated(gameId, userId, DateTimeOffset.UtcNow);

        _session.Events.StartStream(gameId, _session.TagEvent(@event));
        await _session.SaveChangesAsync(cancellationToken);

        return gameId;
    }
}
