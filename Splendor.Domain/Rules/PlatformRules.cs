namespace Splendor.Domain.Rules;

/// <summary>Platform rules (who may do what), kept apart from the Splendor gameplay rules.</summary>
public static class PlatformRules
{
    public const int MaxActiveGames = 2;
    public const int MaxActiveGamesForBots = 40;
    public const int MaxOpenCreatedGames = 2;
    public const int MaxOpenCreatedGamesForBots = 20;
    public const int MaxTotalCreatedGamesForGuests = 10;
    public const int DefaultTurnSeconds = 60;
    public const int MinTurnSeconds = 30;
    public const int MaxTurnSeconds = 300;
    public const int MaxListedGames = 100;
    public static readonly TimeSpan StaleGameAge = TimeSpan.FromHours(48);
    public static readonly TimeSpan DeletedGameRetention = TimeSpan.FromDays(7);

    public static bool CanDeleteGame(Caller caller, string creatorId) =>
        caller.IsAdmin || caller.UserId.Value == creatorId;

    public static int MaxActiveGamesFor(Caller caller) => caller.IsBot ? MaxActiveGamesForBots : MaxActiveGames;

    public static bool CanJoinAnotherGame(Caller caller, int activeGames) => activeGames < MaxActiveGamesFor(caller);

    public static int MaxOpenCreatedGamesFor(Caller caller) => caller.IsBot ? MaxOpenCreatedGamesForBots : MaxOpenCreatedGames;

    public static bool CanCreateGame(Caller caller, int openCreatedGames) => openCreatedGames < MaxOpenCreatedGamesFor(caller);

    /// <summary>Guests may not create games endlessly (create + delete would never hit the open-games limit).</summary>
    public static bool WithinTotalCreatedLimit(Caller caller, int totalCreatedGames) =>
        !caller.IsGuest || totalCreatedGames < MaxTotalCreatedGamesForGuests;

    public static bool IsValidTurnDuration(int seconds) => seconds is >= MinTurnSeconds and <= MaxTurnSeconds;

    public static bool IsStale(DateTimeOffset lastActivity, DateTimeOffset now) => now - lastActivity > StaleGameAge;
}
