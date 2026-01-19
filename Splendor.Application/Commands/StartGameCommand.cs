using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain.Aggregates;

namespace Splendor.Application.Commands;

public record StartGameCommand(Guid GameId, string OwnerId) : IAuthoredCommand, IRequest;

public class StartGameCommandHandler : IRequestHandler<StartGameCommand>
{
    private readonly IEventStore _eventStore;
    private readonly IGameReadModelProjector _projector;

    public StartGameCommandHandler(IEventStore eventStore, IGameReadModelProjector projector)
    {
        _eventStore = eventStore;
        _projector = projector;
    }

    public async Task Handle(StartGameCommand request, CancellationToken cancellationToken)
    {
        var game = await _eventStore.LoadAsync<Game>(request.GameId, cancellationToken);
        if (game == null) throw new Exception("Game not found");

        var events = game.StartGame(request.OwnerId);

        // 1. Write to Event Store (Source of Truth)
        await _eventStore.AppendAsync(request.GameId, events, cancellationToken);
        await _eventStore.SaveChangesAsync(cancellationToken);

        // 2. Sync to Read Model (SQL Server)
        await _projector.ProjectAsync(events, cancellationToken);
    }
}
