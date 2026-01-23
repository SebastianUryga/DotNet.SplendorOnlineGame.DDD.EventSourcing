namespace Splendor.Application.Messages;

public record GameUpdatedMessage(
    Guid GameId,
    string EventType,    // e.g. "GameStarted", "GemsTaken"
    long Version
);
