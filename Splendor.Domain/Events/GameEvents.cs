using Splendor.Domain.Common;
using Splendor.Domain.ValueObjects;

namespace Splendor.Domain.Events;

public record GameCreated(Guid GameId, string CreatorId, DateTimeOffset Timestamp) : IDomainEvent;
public record PlayerJoined(Guid GameId, string PlayerId, string OwnerId, string Name, DateTimeOffset Timestamp) : IDomainEvent;
public record GameStarted(Guid GameId, DateTimeOffset Timestamp) : IDomainEvent;
public record TurnStarted(Guid GameId, string PlayerId, DateTimeOffset Timestamp) : IDomainEvent;
public record GemsTaken(Guid GameId, string PlayerId, GemCollection Gems, DateTimeOffset Timestamp) : IDomainEvent;
public record TurnEnded(Guid GameId, string PlayerId, DateTimeOffset Timestamp) : IDomainEvent;
