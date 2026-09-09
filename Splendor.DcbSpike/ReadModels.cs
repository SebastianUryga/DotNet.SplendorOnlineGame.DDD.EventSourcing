using Marten.Events.Aggregation;
using Marten.Events.Projections;
using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike;

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

public class SplendorBoardView
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "Created";
    public GemCollection MarketGems { get; set; } = GemCollection.Empty;
    public string? CurrentPlayerId { get; set; }
    public List<string> Market1 { get; set; } = new();
    public List<string> Market2 { get; set; } = new();
    public List<string> Market3 { get; set; } = new();
    public List<PlayerBoardView> Players { get; set; } = new();
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public class PlayerBoardView
{
    public string Id { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GemCollection Gems { get; set; } = GemCollection.Empty;
    public List<string> OwnedCardIds { get; set; } = new();
    public List<string> ReservedCardIds { get; set; } = new();
}

public class UserStatsView
{
    public string Id { get; set; } = string.Empty;
    public int GamesJoined { get; set; }
    public int GamesWon { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public partial class GameSummaryProjection : MultiStreamProjection<GameSummaryView, Guid>
{
    public GameSummaryProjection()
    {
        Identity<GameCreated>(e => e.GameId);
        Identity<PlayerJoined>(e => e.GameId);
        Identity<GameStarted>(e => e.GameId);
        Identity<TurnStarted>(e => e.GameId);
        Identity<GameFinished>(e => e.GameId);
        Identity<GameDeleted>(e => e.GameId);
    }

    public GameSummaryView Create(GameCreated e) => new()
    {
        Id = e.GameId,
        Status = "Created",
        UpdatedAt = e.Timestamp,
        Version = 1
    };

    public void Apply(PlayerJoined e, GameSummaryView view)
    {
        view.PlayerCount++;
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GameStarted e, GameSummaryView view)
    {
        view.Status = "Started";
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(TurnStarted e, GameSummaryView view)
    {
        view.CurrentPlayerId = e.PlayerId;
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GameFinished e, GameSummaryView view)
    {
        view.Status = "Finished";
        view.CurrentPlayerId = null;
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GameDeleted e, GameSummaryView view)
    {
        view.Status = "Deleted";
        SetProjectionMetadata(view, e.Timestamp);
    }

    private static void SetProjectionMetadata(GameSummaryView view, DateTimeOffset timestamp)
    {
        view.UpdatedAt = timestamp;
        view.Version++;
    }
}

public partial class SplendorBoardProjection : MultiStreamProjection<SplendorBoardView, Guid>
{
    public SplendorBoardProjection()
    {
        Identity<GameCreated>(e => e.GameId);
        Identity<PlayerJoined>(e => e.GameId);
        Identity<GameStarted>(e => e.GameId);
        Identity<TurnStarted>(e => e.GameId);
        Identity<GemsTaken>(e => e.GameId);
        Identity<GemLimitResolved>(e => e.GameId);
        Identity<CardPurchased>(e => e.GameId);
        Identity<CardRevealed>(e => e.GameId);
        Identity<CardReserved>(e => e.GameId);
        Identity<GameFinished>(e => e.GameId);
        Identity<GameDeleted>(e => e.GameId);
    }

    public SplendorBoardView Create(GameCreated e) => new()
    {
        Id = e.GameId,
        Status = "Created",
        UpdatedAt = e.Timestamp,
        Version = 1
    };

    public void Apply(PlayerJoined e, SplendorBoardView view)
    {
        view.Players.Add(new PlayerBoardView
        {
            Id = e.PlayerId,
            OwnerId = e.OwnerId,
            Name = e.Name
        });
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GameStarted e, SplendorBoardView view)
    {
        view.Status = "Started";
        view.MarketGems = e.MarketGems;
        view.Market1 = e.Market1.ToList();
        view.Market2 = e.Market2.ToList();
        view.Market3 = e.Market3.ToList();
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(TurnStarted e, SplendorBoardView view)
    {
        view.CurrentPlayerId = e.PlayerId;
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GemsTaken e, SplendorBoardView view)
    {
        view.MarketGems -= e.Gems;

        var player = view.Players.FirstOrDefault(p => p.Id == e.PlayerId);
        if (player != null)
        {
            player.Gems += e.Gems;
        }

        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GemLimitResolved e, SplendorBoardView view)
    {
        view.MarketGems += e.ReturnedGems;

        var player = view.Players.FirstOrDefault(p => p.Id == e.PlayerId);
        if (player != null)
        {
            player.Gems -= e.ReturnedGems;
        }

        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(CardPurchased e, SplendorBoardView view)
    {
        var player = view.Players.FirstOrDefault(p => p.Id == e.PlayerId);
        if (player != null)
        {
            player.Gems -= e.PaidGems;
            player.OwnedCardIds.Add(e.CardId);
            player.ReservedCardIds.Remove(e.CardId);
        }

        view.MarketGems += e.PaidGems;
        RemoveMarketCard(view, e.CardId);
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(CardRevealed e, SplendorBoardView view)
    {
        if (e.Level == 1) view.Market1.Add(e.CardId);
        if (e.Level == 2) view.Market2.Add(e.CardId);
        if (e.Level == 3) view.Market3.Add(e.CardId);
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(CardReserved e, SplendorBoardView view)
    {
        RemoveMarketCard(view, e.CardId);

        var player = view.Players.FirstOrDefault(p => p.Id == e.PlayerId);
        if (player != null)
        {
            player.ReservedCardIds.Add(e.CardId);
        }

        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GameFinished e, SplendorBoardView view)
    {
        view.Status = "Finished";
        view.CurrentPlayerId = null;
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GameDeleted e, SplendorBoardView view)
    {
        view.Status = "Deleted";
        SetProjectionMetadata(view, e.Timestamp);
    }

    private static void SetProjectionMetadata(SplendorBoardView view, DateTimeOffset timestamp)
    {
        view.UpdatedAt = timestamp;
        view.Version++;
    }

    private static void RemoveMarketCard(SplendorBoardView view, string cardId)
    {
        var card = CardCatalog.GetById(cardId);
        if (card?.Level == 1) view.Market1.Remove(cardId);
        if (card?.Level == 2) view.Market2.Remove(cardId);
        if (card?.Level == 3) view.Market3.Remove(cardId);
    }
}

public partial class UserStatsProjection : MultiStreamProjection<UserStatsView, string>
{
    public UserStatsProjection()
    {
        Identity<PlayerJoined>(e => e.OwnerId);
        Identity<GameFinished>(e => e.WinnerOwnerId);
    }

    public UserStatsView Create(PlayerJoined e) => new()
    {
        Id = e.OwnerId,
        GamesJoined = 1,
        UpdatedAt = e.Timestamp,
        Version = 1
    };

    public UserStatsView Create(GameFinished e) => new()
    {
        Id = e.WinnerOwnerId,
        GamesWon = 1,
        UpdatedAt = e.Timestamp,
        Version = 1
    };

    public void Apply(PlayerJoined e, UserStatsView view)
    {
        view.GamesJoined++;
        SetProjectionMetadata(view, e.Timestamp);
    }

    public void Apply(GameFinished e, UserStatsView view)
    {
        view.GamesWon++;
        SetProjectionMetadata(view, e.Timestamp);
    }

    private static void SetProjectionMetadata(UserStatsView view, DateTimeOffset timestamp)
    {
        view.UpdatedAt = timestamp;
        view.Version++;
    }
}
