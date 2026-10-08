namespace Splendor.Contracts.Messages;

/// <summary>Hourly trigger: find inactive games to delete.</summary>
public record CleanUpGamesMessage;

/// <summary>Logical delete (GameDeleted event) of a game with no recent activity.</summary>
public record DeleteStaleGameMessage(Guid GameId);

// DISABLED for now, see HardDeleteGameCommand.cs.
// /// <summary>Physical removal of a game already deleted: its event stream and projected documents.</summary>
// public record HardDeleteGameMessage(Guid GameId);
