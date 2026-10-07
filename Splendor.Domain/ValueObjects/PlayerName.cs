namespace Splendor.Domain.ValueObjects;

public readonly record struct PlayerName
{
    public const int MaxLength = 20;

    public string Value { get; }

    private PlayerName(string value) => Value = value;

    public static PlayerName Create(string? raw)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (value.Length is 0 or > MaxLength)
            throw new ArgumentException($"Player name must be 1-{MaxLength} characters.");
        return new PlayerName(value);
    }

    public override string ToString() => Value;
}
