namespace Splendor.Application.Common.Interfaces;

public interface IEventStore
{
    Task AppendAsync(Guid streamId, IEnumerable<object> events, CancellationToken cancellationToken = default);
    Task<T?> LoadAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : class, new();
    Task<IReadOnlyList<object>> FetchStreamAsync(Guid streamId, CancellationToken cancellationToken = default);
    Task<T?> LoadDocumentAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : class;
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
