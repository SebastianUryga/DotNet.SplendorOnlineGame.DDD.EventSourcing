using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain.Aggregates;

namespace Splendor.Application.Commands;

public record JoinGameCommand : IAuthoredCommand, IRequest
{
    public Guid GameId { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public class JoinGameCommandHandler : IRequestHandler<JoinGameCommand>
{
    private readonly IEventStore _eventStore;

    public JoinGameCommandHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task Handle(JoinGameCommand request, CancellationToken cancellationToken)
    {
        var game = await _eventStore.LoadAsync<Game>(request.GameId, cancellationToken);
        if (game == null) throw new Exception("Game not found");

        var events = game.JoinGame(request.OwnerId, request.Name).ToList();

        await _eventStore.AppendAsync(request.GameId, events, cancellationToken);
        await _eventStore.SaveChangesAsync(cancellationToken);
    }
}
