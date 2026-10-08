using Splendor.Domain.Rules;
using MediatR;
using Marten;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.Events;
using Splendor.Domain.Events;

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
        var gameId = Guid.NewGuid();
        var @event = new GameCreated(gameId, command.Caller.UserId.Value, DateTimeOffset.UtcNow);

        _session.Events.StartStream(gameId, _session.TagEvent(@event));
        await _session.SaveChangesAsync(cancellationToken);

        return gameId;
    }
}
