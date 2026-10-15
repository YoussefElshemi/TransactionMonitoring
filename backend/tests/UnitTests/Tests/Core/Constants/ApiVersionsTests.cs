using Core.Constants;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Constants;

public class ApiVersionsTests : BaseTestClass
{
    [Fact]
    public void EveryDeprecatedVersion_HasASunsetDate()
    {
        // Arrange
        var undated = ApiVersions.All
            .Where(x => x.DeprecatedOn.HasValue && !x.SunsetOn.HasValue)
            .Select(x => x.Version.ToString())
            .ToList();

        // Assert
        undated.Should().BeEmpty(
            "a deprecation without a date tells a customer to move and not by when. The sunset is " +
            "published with the deprecation, not after it. Undated: {0}",
            string.Join(", ", undated));
    }

    [Fact]
    public void EverySunsetDate_GivesAtLeastThePublishedNotice()
    {
        // Arrange
        var short_ = ApiVersions.All
            .Where(x => x.DeprecatedOn.HasValue && x.SunsetOn.HasValue)
            .Where(x => x.SunsetOn!.Value < ((DateOnly)x.DeprecatedOn!.Value).AddMonths(ApiVersions.MinimumNoticeMonths))
            .Select(x => x.Version.ToString())
            .ToList();

        // Assert
        short_.Should().BeEmpty(
            "a version is served for at least {0} months after it is deprecated. Too soon: {1}",
            ApiVersions.MinimumNoticeMonths,
            string.Join(", ", short_));
    }

    [Fact]
    public void NoVersion_HasASunsetDateWithoutBeingDeprecated()
    {
        // Arrange
        var silent = ApiVersions.All.Where(x => x.SunsetOn.HasValue && !x.DeprecatedOn.HasValue);

        // Assert
        silent.Should().BeEmpty();
    }

    [Fact]
    public void TheCurrentVersion_IsNotDeprecated()
    {
        // Arrange
        var current = ApiVersions.All.Single(x => x.Version == ApiVersions.Current);

        // Assert
        current.DeprecatedOn.Should().BeNull();
    }

    [Fact]
    public void EveryVersion_IsNamedAfterTheDayItWasReleased()
    {
        // Arrange
        var misnamed = ApiVersions.All
            .Where(x => x.Version.ToString() != ((DateOnly)x.ReleasedOn).ToString("yyyy-MM-dd"))
            .Select(x => x.Version.ToString());

        // Assert
        misnamed.Should().BeEmpty();
    }

    [Fact]
    public void NoVersion_IsDeclaredTwice()
    {
        // Arrange
        var versions = ApiVersions.All.Select(x => x.Version.ToString());

        // Assert
        versions.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void TheFirstVersion_IsStillServed()
    {
        // Arrange
        var versions = ApiVersions.All.Select(x => x.Version.ToString());

        // Assert
        // Clients predating versioning were backfilled to this value by the AddApiClientApiVersion migration.
        versions.Should().Contain(ApiVersions.V20261014);
    }

    [Fact]
    public void ThePolicy_StatesBothHalvesOfTheRule()
    {
        // Arrange & Act
        ApiVersions.BreakingChanges.Should().NotBeEmpty();
        ApiVersions.NonBreakingChanges.Should().NotBeEmpty();
        ApiVersions.MinimumNoticeMonths.Should().BeGreaterThan(0);
    }
}
