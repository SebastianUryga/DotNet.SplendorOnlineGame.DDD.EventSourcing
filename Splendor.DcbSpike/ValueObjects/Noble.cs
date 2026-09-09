using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike.ValueObjects;

public record Noble(
    string Id,
    int PrestigePoints,
    GemCollection Requirements,
    string? Name = null
);
