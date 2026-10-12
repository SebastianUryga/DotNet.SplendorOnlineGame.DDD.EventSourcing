namespace Splendor.Domain.ValueObjects;

public readonly record struct GameName
{
    public const int MaxLength = 40;

    public string Value { get; }

    private GameName(string value) => Value = value;

    public static GameName Create(string? raw)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (value.Length is 0 or > MaxLength)
            throw new ArgumentException($"Game name must be 1-{MaxLength} characters.");
        return new GameName(value);
    }

    public override string ToString() => Value;
}
