namespace Splendor.Application.Common.Interfaces;

public interface IGameReadModelProjector
{
    Task ProjectAsync(IEnumerable<object> events, CancellationToken cancellationToken);
}
