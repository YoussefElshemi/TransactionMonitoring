using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.ValueObjects;

public class DisplayTimeZoneTests : BaseTestClass
{
    [Theory]
    [InlineData("Europe/London")]
    [InlineData("GMT Standard Time")]
    public void Constructor_WhicheverSpellingArrives_StoresTheIanaId(string value)
    {
        // Arrange
        // Act
        var timeZone = new DisplayTimeZone(value);

        // Assert
        ((string)timeZone).Should().Be("Europe/London");
    }

    [Fact]
    public void Resolve_GivenAStoredZone_ReturnsAZoneThisHostCanConvertWith()
    {
        // Arrange
        var timeZone = new DisplayTimeZone("Asia/Tokyo");
        var instant = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

        // Act
        var local = TimeZoneInfo.ConvertTime(instant, timeZone.Resolve());

        // Assert
        local.Hour.Should().Be(18);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Mars/Olympus_Mons")]
    public void Constructor_GivenAZoneThisHostCannotFind_Throws(string value)
    {
        // Arrange
        // Act
        var act = () => new DisplayTimeZone(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
