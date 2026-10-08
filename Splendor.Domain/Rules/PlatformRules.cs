namespace Splendor.Domain.Rules;

/// <summary>Platform rules (who may do what), kept apart from the Splendor gameplay rules.</summary>
public static class PlatformRules
{
    public const int MaxActiveGames = 2;

    public static bool CanDeleteGame(Caller caller, string creatorId) =>
        caller.IsAdmin || caller.UserId.Value == creatorId;

    public static bool CanJoinAnotherGame(int activeGames) => activeGames < MaxActiveGames;
}
