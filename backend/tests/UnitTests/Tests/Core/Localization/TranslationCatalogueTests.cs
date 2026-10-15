using System.Text.RegularExpressions;
using Core.Enums;
using Core.Localization;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Localization;

public class TranslationCatalogueTests : BaseTestClass
{
    private static readonly Regex Placeholder = new(@"\{(\d+)\}", RegexOptions.Compiled);

    [Fact]
    public void Catalogue_ForEveryLanguage_HasEveryKey()
    {
        // Arrange
        // Act
        var missing = TranslationCatalogue.Supported
            .SelectMany(language => TranslationKeys.All
                .Where(key => !TranslationCatalogue.For(language).ContainsKey(key))
                .Select(key => language + ":" + key))
            .ToList();

        // Assert
        missing.Should().BeEmpty();
    }

    [Fact]
    public void Catalogue_ForEveryLanguage_HasNoKeyTheProductDoesNotAskFor()
    {
        // Arrange
        var known = TranslationKeys.All.ToHashSet(StringComparer.Ordinal);

        // Act
        var unexpected = TranslationCatalogue.Supported
            .SelectMany(language => TranslationCatalogue.For(language).Keys
                .Where(key => !known.Contains(key))
                .Select(key => language + ":" + key))
            .ToList();

        // Assert
        unexpected.Should().BeEmpty();
    }

    [Fact]
    public void Catalogue_ForEveryLanguage_UsesTheSamePlaceholdersAsEnglish()
    {
        // Arrange
        var mismatched = new List<string>();

        // Act
        foreach (var key in TranslationKeys.All)
        {
            var expected = PlaceholdersIn(TranslationCatalogue.Find(Language.En, key));

            mismatched.AddRange(TranslationCatalogue.Supported
                .Where(language => !PlaceholdersIn(TranslationCatalogue.Find(language, key)).SetEquals(expected))
                .Select(language => language + ":" + key));
        }

        // Assert
        mismatched.Should().BeEmpty();
    }

    [Fact]
    public void Find_GivenALanguageWithNoTranslations_FallsBackToEnglish()
    {
        // Arrange
        const Language unwritten = (Language)999;

        // Act
        var translated = TranslationCatalogue.Find(unwritten, TranslationKeys.LinePlatformFee);

        // Assert
        translated.Should().Be(TranslationCatalogue.Find(Language.En, TranslationKeys.LinePlatformFee));
    }

    [Fact]
    public void Find_GivenAKeyNothingHasWritten_ReturnsTheKey()
    {
        // Arrange
        // Act
        var translated = TranslationCatalogue.Find(Language.Fr, "nothing.has.written.this");

        // Assert
        translated.Should().Be("nothing.has.written.this");
    }

    private static HashSet<string> PlaceholdersIn(string template)
    {
        return Placeholder.Matches(template).Select(x => x.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
    }
}
