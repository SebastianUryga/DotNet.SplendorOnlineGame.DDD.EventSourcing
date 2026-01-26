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

    public StartGameCommandHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task Handle(StartGameCommand request, CancellationToken cancellationToken)
    {
        var game = await _eventStore.LoadAsync<Game>(request.GameId, cancellationToken);
        if (game == null) throw new Exception("Game not found");

        var events = game.StartGame(request.OwnerId).ToList();

        await _eventStore.AppendAsync(request.GameId, events, cancellationToken);
        await _eventStore.SaveChangesAsync(cancellationToken);
    }
}
