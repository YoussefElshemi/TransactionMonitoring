using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.ValueObjects;

public class ApiVersionTests : BaseTestClass
{
    [Fact]
    public void Constructor_GivenADate_Accepts()
    {
        // Arrange
        // Act
        var apiVersion = new ApiVersion("2026-10-14");

        // Assert
        ((string)apiVersion).Should().Be("2026-10-14");
    }

    [Theory]
    [InlineData("")]
    [InlineData("v1")]
    [InlineData("1")]
    [InlineData("2026-13-01")]
    [InlineData("2026-02-30")]
    [InlineData("14-10-2026")]
    [InlineData("2026-10-14T00:00:00Z")]
    [InlineData("2026-1-4")]
    public void Constructor_GivenSomethingThatIsNotADate_Throws(string value)
    {
        // Arrange
        // Act
        var act = () => new ApiVersion(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("latest")]
    public void TryParse_GivenSomethingThatIsNotAVersion_ReturnsFalse(string? value)
    {
        // Arrange
        // Act
        var parsed = ApiVersion.TryParse(value, out _);

        // Assert
        parsed.Should().BeFalse();
    }

    [Fact]
    public void CompareTo_OrdersVersionsByDate()
    {
        // Arrange
        ApiVersion[] versions = [new("2027-03-01"), new("2026-10-14"), new("2026-12-31")];

        // Act
        var ordered = versions.Order().Select(x => x.ToString());

        // Assert
        ordered.Should().ContainInOrder("2026-10-14", "2026-12-31", "2027-03-01");
    }
}
