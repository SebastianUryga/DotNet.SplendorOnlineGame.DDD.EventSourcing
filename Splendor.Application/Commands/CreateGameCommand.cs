using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain.Events;

namespace Splendor.Application.Commands;

public record CreateGameCommand : IAuthoredCommand, IRequest<Guid>
{
    public string OwnerId { get; init; } = string.Empty;
}

public class CreateGameCommandHandler : IRequestHandler<CreateGameCommand, Guid>
{
    private readonly IEventStore _eventStore;

    public CreateGameCommandHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<Guid> Handle(CreateGameCommand request, CancellationToken cancellationToken)
    {
        var gameId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var events = new List<object>
        {
            new GameCreated(gameId, request.OwnerId, timestamp)
        };

        await _eventStore.AppendAsync(gameId, events, cancellationToken);
        await _eventStore.SaveChangesAsync(cancellationToken);

        return gameId;
    }
}
