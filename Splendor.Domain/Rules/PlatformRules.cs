namespace Splendor.Domain.Rules;

/// <summary>Platform rules (who may do what), kept apart from the Splendor gameplay rules.</summary>
public static class PlatformRules
{
    public const int MaxActiveGames = 2;
    public const int MaxOpenCreatedGames = 2;
    public static readonly TimeSpan StaleGameAge = TimeSpan.FromHours(48);
    public static readonly TimeSpan DeletedGameRetention = TimeSpan.FromDays(7);

    public static bool CanDeleteGame(Caller caller, string creatorId) =>
        caller.IsAdmin || caller.UserId.Value == creatorId;

    public static bool CanJoinAnotherGame(int activeGames) => activeGames < MaxActiveGames;

    public static bool CanCreateGame(int openCreatedGames) => openCreatedGames < MaxOpenCreatedGames;

    public static bool IsStale(DateTimeOffset lastActivity, DateTimeOffset now) => now - lastActivity > StaleGameAge;
}
