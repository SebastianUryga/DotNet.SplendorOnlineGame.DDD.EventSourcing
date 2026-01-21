using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain.Aggregates;

namespace Splendor.Application.Commands;

public record StartGameCommand : IAuthoredCommand, IRequest
{
    public Guid GameId { get; init; }
    public string OwnerId { get; init; } = string.Empty;

    public StartGameCommand() { }
    public StartGameCommand(Guid gameId, string ownerId)
    {
        GameId = gameId;
        OwnerId = ownerId;
    }
}

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

        var events = game.StartGame(request.OwnerId).ToList();

        // 1. Write to Event Store (Source of Truth)
        await _eventStore.AppendAsync(request.GameId, events, cancellationToken);
        await _eventStore.SaveChangesAsync(cancellationToken);

        // 2. Sync to Read Model (SQL Server)
        await _projector.ProjectAsync(events, cancellationToken);
    }
}
