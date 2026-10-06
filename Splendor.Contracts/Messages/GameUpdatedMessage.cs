namespace Splendor.Contracts.Messages;

public record GameUpdatedMessage(
    Guid GameId,
    string EventType,    // e.g. "GameStarted", "GemsTaken"
    long StreamVersion,
    string? Data = null  // the domain event serialized as JSON
    );
