using System.Text.Json;
using FluentAssertions;
using Splendor.Application.Snapshots;
using Splendor.Domain.Events;
using Xunit;

namespace Splendor.UnitTests;

// Renamed C# members must keep reading JSON already stored in Neon.
public class StoredJsonCompatibilityTests
{
    [Fact]
    public void Snapshot_reads_legacy_property_names()
    {
        var json = """{"Id":"6f0c1d5e-0000-0000-0000-000000000001","CreatorId":"creator-1","Players":{"p1":{"OwnerId":"owner-1","Name":"Alice"}}}""";

        var state = JsonSerializer.Deserialize<SplendorGameState>(json)!;

        state.GameCreatorId.Should().Be("creator-1");
        state.Players["p1"].PlayerOwnerId.Should().Be("owner-1");
    }

    [Fact]
    public void Event_reads_and_writes_legacy_owner_property()
    {
        var joined = JsonSerializer.Deserialize<PlayerJoined>(
            """{"GameId":"6f0c1d5e-0000-0000-0000-000000000001","PlayerId":"p1","OwnerId":"owner-1","Name":"Alice","Timestamp":"2026-01-01T00:00:00+00:00"}""")!;

        joined.PlayerOwnerId.Should().Be("owner-1");
        JsonSerializer.Serialize(joined).Should().Contain("\"OwnerId\":\"owner-1\"");
    }
}
