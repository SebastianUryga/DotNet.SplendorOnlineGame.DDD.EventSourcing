using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain.Aggregates;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record TakeGemsCommand : IAuthoredCommand, IRequest
{
    public Guid GameId { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string PlayerId { get; init; } = string.Empty;
    public int Diamond { get; init; }
    public int Sapphire { get; init; }
    public int Emerald { get; init; }
    public int Ruby { get; init; }
    public int Onyx { get; init; }
    public int Gold { get; init; }
}

public class TakeGemsCommandHandler : IRequestHandler<TakeGemsCommand>
{
    private readonly IEventStore _eventStore;

    public TakeGemsCommandHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task Handle(TakeGemsCommand request, CancellationToken cancellationToken)
    {
        var game = await _eventStore.LoadAsync<Game>(request.GameId, cancellationToken);
        if (game == null) throw new Exception("Game not found");

        var gems = new GemCollection(request.Diamond, request.Sapphire, request.Emerald, request.Ruby, request.Onyx, request.Gold);

        var events = game.TakeGems(request.OwnerId, request.PlayerId, gems).ToList();

        await _eventStore.AppendAsync(request.GameId, events, cancellationToken);
        await _eventStore.SaveChangesAsync(cancellationToken);
    }
}
