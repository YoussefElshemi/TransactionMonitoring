using Core.Constants;
using Core.Models.ApiVersions;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Services;

public class ApiVersionServiceTests : BaseTestClass
{
    private const string Oldest = "2026-10-14";
    private const string Middle = "2027-03-01";
    private const string Newest = "2027-09-01";

    private readonly ApiVersionService _sut = new(
    [
        Define(Middle),
        Define(Newest),
        Define(Oldest, deprecatedOn: new DateOnly(2027, 3, 1), sunsetOn: new DateOnly(2028, 3, 1))
    ]);

    [Fact]
    public void Current_IsTheNewestVersionHoweverTheListIsOrdered()
    {
        // Arrange & Act
        var current = _sut.Current;

        // Assert
        current.Version.ToString().Should().Be(Newest);
    }

    [Fact]
    public void Earliest_IsTheOldestVersionStillServed()
    {
        // Arrange & Act
        var earliest = _sut.Earliest;

        // Assert
        earliest.Version.ToString().Should().Be(Oldest);
    }

    [Fact]
    public void Resolve_GivenNoPin_ServesTheCurrentVersion()
    {
        // Arrange & Act
        var resolved = _sut.Resolve(null);

        // Assert
        resolved.Version.ToString().Should().Be(Newest);
    }

    [Fact]
    public void Resolve_GivenAPinThatIsServed_ServesExactlyThat()
    {
        // Arrange & Act
        var resolved = _sut.Resolve(new ApiVersion(Middle));

        // Assert
        resolved.Version.ToString().Should().Be(Middle);
    }

    [Fact]
    public void Resolve_GivenAPinThatHasBeenRetired_ServesTheEarliestRatherThanRefusing()
    {
        // Arrange
        var retired = new ApiVersion("2025-01-01");

        // Act
        var resolved = _sut.Resolve(retired);

        // Assert
        resolved.Version.ToString().Should().Be(Oldest);
    }

    [Fact]
    public void Find_GivenAVersionThatIsNotServed_ReturnsNull()
    {
        // Arrange & Act
        var found = _sut.Find(new ApiVersion("2025-01-01"));

        // Assert
        found.Should().BeNull();
    }

    [Fact]
    public void GetCatalogue_PublishesTheVersionsAndThePolicyTheyAreReleasedUnder()
    {
        // Arrange & Act
        var catalogue = _sut.GetCatalogue();

        // Assert
        catalogue.Current.ToString().Should().Be(Newest);
        catalogue.Versions.Should().HaveCount(3);
        ((int)catalogue.MinimumNotice).Should().Be(ApiVersions.MinimumNoticeMonths);
        catalogue.BreakingChanges.Should().BeEquivalentTo(ApiVersions.BreakingChanges);
        catalogue.NonBreakingChanges.Should().BeEquivalentTo(ApiVersions.NonBreakingChanges);
    }

    [Fact]
    public void Constructor_GivenNoVersions_Throws()
    {
        // Arrange & Act
        var act = () => new ApiVersionService([]);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    private static ApiVersionDefinition Define(string version, DateOnly? deprecatedOn = null, DateOnly? sunsetOn = null)
    {
        return new ApiVersionDefinition
        {
            Version = new ApiVersion(version),
            ReleasedOn = new ReleasedOn(DateOnly.ParseExact(version, ApiVersion.Format)),
            DeprecatedOn = deprecatedOn.HasValue ? new DeprecatedOn(deprecatedOn.Value) : null,
            SunsetOn = sunsetOn.HasValue ? new SunsetOn(sunsetOn.Value) : null,
            Summary = new ApiVersionSummary("A version declared for a test.")
        };
    }
}
