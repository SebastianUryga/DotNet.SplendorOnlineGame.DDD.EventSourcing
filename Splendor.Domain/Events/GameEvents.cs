using Splendor.Domain.Common;
using Splendor.Domain.ValueObjects;

namespace Splendor.Domain.Events;

public record GameCreated(Guid GameId, Guid CreatorId, DateTimeOffset Timestamp) : IDomainEvent;
public record PlayerJoined(Guid GameId, Guid PlayerId, string Name, DateTimeOffset Timestamp) : IDomainEvent;
public record GameStarted(Guid GameId, DateTimeOffset Timestamp) : IDomainEvent;
public record TurnStarted(Guid GameId, Guid PlayerId, DateTimeOffset Timestamp) : IDomainEvent;
public record GemsTaken(Guid GameId, Guid PlayerId, GemCollection Gems, DateTimeOffset Timestamp) : IDomainEvent;
public record TurnEnded(Guid GameId, Guid PlayerId, DateTimeOffset Timestamp) : IDomainEvent;
