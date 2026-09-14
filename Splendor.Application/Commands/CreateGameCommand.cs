using MediatR;
using Marten;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain.Events;

namespace Splendor.Application.Commands;

public record CreateGameCommand : IAuthoredCommand, IRequest<Guid>
{
    public string OwnerId { get; init; } = string.Empty;
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
        var @event = new GameCreated(gameId, command.OwnerId, DateTimeOffset.UtcNow);

        _session.Events.StartStream(gameId, @event);
        await _session.SaveChangesAsync(cancellationToken);

        return gameId;
    }
}
