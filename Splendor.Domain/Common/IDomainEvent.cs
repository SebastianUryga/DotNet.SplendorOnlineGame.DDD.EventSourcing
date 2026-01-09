namespace Splendor.Domain.Common;

public interface IDomainEvent
{
    Guid GameId { get; }
    DateTimeOffset Timestamp { get; }
}
