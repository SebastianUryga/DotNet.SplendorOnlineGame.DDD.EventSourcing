namespace Splendor.Domain.ValueObjects;

public record Card(
    string Id,
    int Level,
    GemType BonusType,
    int PrestigePoints,
    GemCollection Cost
);
