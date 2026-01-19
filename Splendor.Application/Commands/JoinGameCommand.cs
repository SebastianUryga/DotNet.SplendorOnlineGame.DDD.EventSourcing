using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain.Aggregates;

namespace Splendor.Application.Commands;

public record JoinGameCommand(Guid GameId, string OwnerId, string Name) : IAuthoredCommand, IRequest;

public class JoinGameCommandHandler : IRequestHandler<JoinGameCommand>
{
    private readonly IEventStore _eventStore;
    private readonly IGameReadModelProjector _projector;

    public JoinGameCommandHandler(IEventStore eventStore, IGameReadModelProjector projector)
    {
        _eventStore = eventStore;
        _projector = projector;
    }

    public async Task Handle(JoinGameCommand request, CancellationToken cancellationToken)
    {
        var game = await _eventStore.LoadAsync<Game>(request.GameId, cancellationToken);
        if (game == null) throw new Exception("Game not found");

        var events = game.JoinGame(request.OwnerId, request.Name);

        await _eventStore.AppendAsync(request.GameId, events, cancellationToken);
        await _eventStore.SaveChangesAsync(cancellationToken);

        await _projector.ProjectAsync(events, cancellationToken);
    }
}
