using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain.Aggregates;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record TakeGemsCommand(Guid GameId, string OwnerId, string PlayerId, int Diamond, int Sapphire, int Emerald, int Ruby, int Onyx, int Gold) : IAuthoredCommand, IRequest;

public class TakeGemsCommandHandler : IRequestHandler<TakeGemsCommand>
{
    private readonly IEventStore _eventStore;
    private readonly IGameReadModelProjector _projector;

    public TakeGemsCommandHandler(IEventStore eventStore, IGameReadModelProjector projector)
    {
        _eventStore = eventStore;
        _projector = projector;
    }

    public async Task Handle(TakeGemsCommand request, CancellationToken cancellationToken)
    {
        var game = await _eventStore.LoadAsync<Game>(request.GameId, cancellationToken);
        if (game == null) throw new Exception("Game not found");

        // Map DTO gems to ValueObject
        var gems = new GemCollection(request.Diamond, request.Sapphire, request.Emerald, request.Ruby, request.Onyx, request.Gold);

        var events = game.TakeGems(request.OwnerId, request.PlayerId, gems);

        await _eventStore.AppendAsync(request.GameId, events, cancellationToken);
        await _eventStore.SaveChangesAsync(cancellationToken);

        await _projector.ProjectAsync(events, cancellationToken);
    }
}
