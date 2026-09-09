namespace Splendor.DcbSpike;

internal interface ITurnCompletionSource
{
    GameStatus Status { get; }
    string? CurrentPlayerId { get; }
    List<string> PlayerOrder { get; }
    List<string> Nobles { get; }
    Dictionary<string, PlayerState> Players { get; }
}
