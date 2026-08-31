using Splendor.Domain.Common;
using Splendor.Domain.ValueObjects;

namespace Splendor.Domain.Events;

public record GameCreated(Guid GameId, string CreatorId, DateTimeOffset Timestamp) : IDomainEvent;
public record PlayerJoined(Guid GameId, string PlayerId, string OwnerId, string Name, DateTimeOffset Timestamp) : IDomainEvent;
public record GameStarted(Guid GameId, List<string> Deck1, List<string> Deck2, List<string> Deck3, List<string> Market1, List<string> Market2, List<string> Market3, List<string> Nobles, DateTimeOffset Timestamp) : IDomainEvent;
public record TurnStarted(Guid GameId, string PlayerId, DateTimeOffset Timestamp) : IDomainEvent;
public record GemsTaken(Guid GameId, string PlayerId, GemCollection Gems, DateTimeOffset Timestamp) : IDomainEvent;
public record GemsOverflowDetected(Guid GameId, string PlayerId, GemCollection CurrentGems, int ExcessCount, DateTimeOffset Timestamp) : IDomainEvent;
public record GemLimitResolved(Guid GameId, string PlayerId, GemCollection ReturnedGems, DateTimeOffset Timestamp) : IDomainEvent;
public record TurnEnded(Guid GameId, string PlayerId, DateTimeOffset Timestamp) : IDomainEvent;
public record CardPurchased(Guid GameId, string PlayerId, string CardId, GemCollection PaidGems, DateTimeOffset Timestamp) : IDomainEvent;
public record CardRevealed(Guid GameId, int Level, string CardId, DateTimeOffset Timestamp) : IDomainEvent;
public record CardReserved(Guid GameId, string PlayerId, string CardId, DateTimeOffset Timestamp) : IDomainEvent;
public record NobleSelectionRequired(Guid GameId, string PlayerId, List<string> EligibleNobleIds, DateTimeOffset Timestamp) : IDomainEvent;
public record NobleAcquired(Guid GameId, string PlayerId, string NobleId, DateTimeOffset Timestamp) : IDomainEvent;
public record GameFinished(Guid GameId, string WinnerId, string WinnerName, int PrestigePoints, DateTimeOffset Timestamp) : IDomainEvent;
public record GameDeleted(Guid GameId, DateTimeOffset Timestamp) : IDomainEvent;
