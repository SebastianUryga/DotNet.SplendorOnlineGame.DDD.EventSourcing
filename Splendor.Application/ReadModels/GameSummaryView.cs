namespace Splendor.Application.ReadModels;

public class GameSummaryView
{
    public Guid Id { get; set; }
    public string GameType { get; set; } = "Splendor";
    public string Status { get; set; } = "Created";
    public int PlayerCount { get; set; }
    public string? CurrentPlayerId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}
