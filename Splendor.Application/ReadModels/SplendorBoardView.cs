using Splendor.Domain.ValueObjects;

namespace Splendor.Application.ReadModels;

public class SplendorBoardView
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "Created";
    public GemCollection MarketGems { get; set; } = GemCollection.Empty;
    public string? CurrentPlayerId { get; set; }
    public string? WinnerId { get; set; }
    public string? WinnerName { get; set; }
    public bool IsGemReturnPending { get; set; }
    public List<string> Market1 { get; set; } = new();
    public List<string> Market2 { get; set; } = new();
    public List<string> Market3 { get; set; } = new();
    public List<string> Nobles { get; set; } = new();
    public string? PlayerIdAwaitingNobleSelection { get; set; }
    public List<string> EligibleNobleIds { get; set; } = new();
    public int Deck1Count { get; set; }
    public int Deck2Count { get; set; }
    public int Deck3Count { get; set; }
    public List<PlayerBoardView> Players { get; set; } = new();
    public DateTimeOffset UpdatedAt { get; set; }
    public long GameVersion { get; set; }
}
