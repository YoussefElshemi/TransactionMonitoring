using Core.Helpers;
using Core.Localization;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Localization;

namespace UnitTests.Tests.Core.Helpers;

public class NotificationMessageBuilderLocalizationTests : BaseTestClass
{
    [Fact]
    public void ForBreachedCase_GivenAFrenchReader_WritesInFrench()
    {
        // Arrange
        var @case = FakeCase.CreateValid(Fixture);
        var translator = new Translator(FakeDisplayPreferences.In("fr-FR", "Europe/Paris"));

        // Act
        var message = NotificationMessageBuilder.ForBreachedCase(@case, translator, MockTimeProvider.GetUtcNow());

        // Assert
        message.Subject.Should().Contain("Échéance dépassée");
        message.Body.Should().Contain("Le dossier");
    }

    [Fact]
    public void ForBreachedCase_GivenAReaderInAnotherZone_RendersTheDeadlineInTheirZoneAndNamesIt()
    {
        // Arrange
        var dueAt = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
        var @case = FakeCase.CreateValid(Fixture) with { DueAt = new DueAt(dueAt) };

        var london = new Translator(FakeDisplayPreferences.In("en-GB", "Europe/London"));
        var tokyo = new Translator(FakeDisplayPreferences.In("en-GB", "Asia/Tokyo"));

        // Act
        var inLondon = NotificationMessageBuilder.ForBreachedCase(@case, london, MockTimeProvider.GetUtcNow());
        var inTokyo = NotificationMessageBuilder.ForBreachedCase(@case, tokyo, MockTimeProvider.GetUtcNow());

        // Assert
        inLondon.Body.Should().Contain("09:00").And.Contain("Europe/London");
        inTokyo.Body.Should().Contain("18:00").And.Contain("Asia/Tokyo");
    }

    [Fact]
    public void ForBreachedCase_GivenALocaleNothingIsWrittenFor_WritesInEnglish()
    {
        // Arrange
        var @case = FakeCase.CreateValid(Fixture);
        var translator = new Translator(FakeDisplayPreferences.In("pt-PT", "Europe/Lisbon"));

        // Act
        var message = NotificationMessageBuilder.ForBreachedCase(@case, translator, MockTimeProvider.GetUtcNow());

        // Assert
        message.Subject.Should().Contain("Breached");
        message.Body.Should().Contain("is still open");
    }
}
