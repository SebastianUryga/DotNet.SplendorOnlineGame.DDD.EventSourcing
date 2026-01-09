using Marten;
using Splendor.Application.Common.Interfaces;

namespace Splendor.Infrastructure.Persistence;

public class MartenEventStore : IEventStore
{
    private readonly IDocumentSession _session;

    public MartenEventStore(IDocumentSession session)
    {
        _session = session;
    }

    public Task AppendAsync(Guid streamId, IEnumerable<object> events, CancellationToken cancellationToken = default)
    {
        _session.Events.Append(streamId, events);
        return Task.CompletedTask;
    }

    public async Task<T?> LoadAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : class, new()
    {
        return await _session.Events.AggregateStreamAsync<T>(id, token: cancellationToken);
    }

    public async Task<IReadOnlyList<object>> FetchStreamAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        var events = await _session.Events.FetchStreamAsync(streamId, token: cancellationToken);
        return events.Select(e => e.Data).ToList();
    }

    public async Task<T?> LoadDocumentAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : class
    {
        return await _session.LoadAsync<T>(id, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _session.SaveChangesAsync(cancellationToken);
    }
}
