namespace Splendor.Contracts.Messages;

public record ExpireTurnMessage(Guid GameId, Guid TurnId, string PlayerId);
