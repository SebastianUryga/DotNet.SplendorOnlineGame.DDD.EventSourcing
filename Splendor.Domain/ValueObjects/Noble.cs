using Splendor.Domain.ValueObjects;

namespace Splendor.Domain.ValueObjects;

public record Noble(
    string Id,
    int PrestigePoints,
    GemCollection Requirements,
    string? Name = null
);
