using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.ValueObjects;

public class LocaleTests : BaseTestClass
{
    [Theory]
    [InlineData("en-GB")]
    [InlineData("fr-FR")]
    [InlineData("de-CH")]
    [InlineData("nl-BE")]
    public void Constructor_GivenALanguageTagTheHostKnows_Accepts(string value)
    {
        // Arrange
        // Act
        var locale = new Locale(value);

        // Assert
        ((string)locale).Should().Be(value);
    }

    [Theory]
    [InlineData("en-gb", "en-GB")]
    [InlineData("FR-fr", "fr-FR")]
    public void Constructor_GivenATagInAnotherCasing_NormalisesToTheCultureSOwnSpelling(string value, string expected)
    {
        // Arrange
        // Act
        var locale = new Locale(value);

        // Assert
        ((string)locale).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("not-a-language")]
    [InlineData("zz-ZZ")]
    public void Constructor_GivenSomethingThatIsNotALanguageTag_Throws(string value)
    {
        // Arrange
        // Act
        var act = () => new Locale(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("fr-BE", Language.Fr)]
    [InlineData("fr-CA", Language.Fr)]
    [InlineData("de-AT", Language.De)]
    [InlineData("nl-NL", Language.Nl)]
    public void Language_GivenARegionalTag_ResolvesToItsPrimarySubtag(string value, Language expected)
    {
        // Arrange
        // Act
        var locale = new Locale(value);

        // Assert
        locale.Language.Should().Be(expected);
    }

    [Theory]
    [InlineData("pt-PT")]
    [InlineData("pl-PL")]
    [InlineData("sv-SE")]
    public void Language_GivenATagNothingHasBeenTranslatedInto_FallsBackToEnglish(string value)
    {
        // Arrange
        // Act
        var locale = new Locale(value);

        // Assert
        locale.Language.Should().Be(Language.En);
    }

    [Fact]
    public void IsKnown_GivenATagLongerThanTheColumn_IsFalse()
    {
        // Arrange
        var tooLong = new string('a', Locale.MaxLength + 1);

        // Act
        var known = Locale.IsKnown(tooLong);

        // Assert
        known.Should().BeFalse();
    }
}
