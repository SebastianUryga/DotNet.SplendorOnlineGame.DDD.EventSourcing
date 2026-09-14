namespace Splendor.Application.ReadModels;

public class UserStatsView
{
    public string Id { get; set; } = string.Empty;
    public int GamesJoined { get; set; }
    public int GamesWon { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}
