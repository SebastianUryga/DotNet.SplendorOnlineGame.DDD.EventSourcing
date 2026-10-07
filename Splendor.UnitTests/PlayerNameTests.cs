using FluentAssertions;
using Splendor.Domain.ValueObjects;
using Xunit;

namespace Splendor.UnitTests;

public class PlayerNameTests
{
    [Fact]
    public void Create_trims_and_accepts_max_length()
    {
        PlayerName.Create("  " + new string('a', PlayerName.MaxLength) + " ").Value.Should().HaveLength(PlayerName.MaxLength);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_rejects_empty(string? raw) =>
        FluentActions.Invoking(() => PlayerName.Create(raw)).Should().Throw<ArgumentException>();

    [Fact]
    public void Create_rejects_too_long() =>
        FluentActions.Invoking(() => PlayerName.Create(new string('a', PlayerName.MaxLength + 1))).Should().Throw<ArgumentException>();
}
