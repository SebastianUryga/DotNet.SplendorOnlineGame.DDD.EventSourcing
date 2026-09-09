using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike.ValueObjects;

public record Card(
    string Id,
    int Level,
    GemType BonusType,
    int PrestigePoints,
    GemCollection Cost
);
