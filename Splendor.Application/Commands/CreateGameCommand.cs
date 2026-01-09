using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain.Events;

namespace Splendor.Application.Commands;

public record CreateGameCommand(Guid CreatorId) : IRequest<Guid>;

public class CreateGameCommandHandler : IRequestHandler<CreateGameCommand, Guid>
{
    private readonly IEventStore _eventStore;
    private readonly IGameReadModelProjector _projector;

    public CreateGameCommandHandler(IEventStore eventStore, IGameReadModelProjector projector)
    {
        _eventStore = eventStore;
        _projector = projector;
    }

    public async Task<Guid> Handle(CreateGameCommand request, CancellationToken cancellationToken)
    {
        var gameId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var events = new List<object>
        {
            new GameCreated(gameId, request.CreatorId, timestamp)
        };

        await _eventStore.AppendAsync(gameId, events, cancellationToken);
        await _eventStore.SaveChangesAsync(cancellationToken);

        await _projector.ProjectAsync(events, cancellationToken);

        return gameId;
    }
}
