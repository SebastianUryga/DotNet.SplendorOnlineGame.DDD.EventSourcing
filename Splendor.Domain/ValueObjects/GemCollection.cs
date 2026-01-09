namespace Splendor.Domain.ValueObjects;

public record GemCollection(int Diamond, int Sapphire, int Emerald, int Ruby, int Onyx, int Gold)
{
    public static GemCollection Empty => new(0, 0, 0, 0, 0, 0);

    public int Total => Diamond + Sapphire + Emerald + Ruby + Onyx + Gold;

    public static GemCollection operator +(GemCollection a, GemCollection b)
    {
        return new GemCollection(
            a.Diamond + b.Diamond,
            a.Sapphire + b.Sapphire,
            a.Emerald + b.Emerald,
            a.Ruby + b.Ruby,
            a.Onyx + b.Onyx,
            a.Gold + b.Gold
        );
    }

    public static GemCollection operator -(GemCollection a, GemCollection b)
    {
         return new GemCollection(
            Math.Max(0, a.Diamond - b.Diamond),
            Math.Max(0, a.Sapphire - b.Sapphire),
            Math.Max(0, a.Emerald - b.Emerald),
            Math.Max(0, a.Ruby - b.Ruby),
            Math.Max(0, a.Onyx - b.Onyx),
            Math.Max(0, a.Gold - b.Gold)
        );
    }
}
