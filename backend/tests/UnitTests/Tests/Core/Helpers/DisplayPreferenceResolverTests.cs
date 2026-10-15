using Core.Configs;
using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class DisplayPreferenceResolverTests : BaseTestClass
{
    private static readonly LocalizationConfig Installation = new()
    {
        DefaultLocale = "en-GB",
        DefaultTimeZone = "Europe/London"
    };

    [Fact]
    public void Resolve_GivenTheReaderHasStatedBoth_TakesTheirs()
    {
        // Arrange
        var readerLocale = new Locale("fr-FR");
        var readerTimeZone = new DisplayTimeZone("Europe/Paris");

        // Act
        var resolved = DisplayPreferenceResolver.Resolve(
            readerLocale, readerTimeZone, new Locale("de-DE"), new DisplayTimeZone("Europe/Berlin"), Installation);

        // Assert
        resolved.Locale.Should().Be(readerLocale);
        resolved.TimeZone.Should().Be(readerTimeZone);
    }

    [Fact]
    public void Resolve_GivenTheReaderHasStatedNothing_TakesTheOrganisationS()
    {
        // Arrange
        // Act
        var resolved = DisplayPreferenceResolver.Resolve(
            null, null, new Locale("de-DE"), new DisplayTimeZone("Europe/Berlin"), Installation);

        // Assert
        ((string)resolved.Locale).Should().Be("de-DE");
        ((string)resolved.TimeZone).Should().Be("Europe/Berlin");
    }

    [Fact]
    public void Resolve_GivenNeitherHasStatedAnything_TakesTheInstallationS()
    {
        // Arrange
        // Act
        var resolved = DisplayPreferenceResolver.Resolve(null, null, null, null, Installation);

        // Assert
        ((string)resolved.Locale).Should().Be("en-GB");
        ((string)resolved.TimeZone).Should().Be("Europe/London");
    }

    [Fact]
    public void Resolve_GivenTheReaderHasStatedOnlyTheLanguage_KeepsTheOrganisationSZone()
    {
        // Arrange
        // Act
        var resolved = DisplayPreferenceResolver.Resolve(
            new Locale("fr-FR"), null, new Locale("en-GB"), new DisplayTimeZone("Europe/London"), Installation);

        // Assert
        ((string)resolved.Locale).Should().Be("fr-FR");
        ((string)resolved.TimeZone).Should().Be("Europe/London");
    }

    [Fact]
    public void Resolve_GivenAnInstallationDefaultThatWillNotParse_FallsBackRatherThanThrowing()
    {
        // Arrange
        var misconfigured = new LocalizationConfig
        {
            DefaultLocale = "not-a-language",
            DefaultTimeZone = "Mars/Olympus_Mons"
        };

        // Act
        var resolved = DisplayPreferenceResolver.Resolve(null, null, null, null, misconfigured);

        // Assert
        ((string)resolved.Locale).Should().Be("en-GB");
        ((string)resolved.TimeZone).Should().Be("Etc/UTC");
    }
}
