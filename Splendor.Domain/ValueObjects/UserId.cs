namespace Splendor.Domain.ValueObjects;

public readonly record struct UserId
{
    public const int MaxLength = 128;

    public string Value { get; }

    private UserId(string value) => Value = value;

    public static UserId Create(string? raw)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (value.Length is 0 or > MaxLength)
            throw new ArgumentException($"User id must be 1-{MaxLength} characters.");
        return new UserId(value);
    }

    public override string ToString() => Value;
}
