using Core.Configs;
using Core.Constants;
using Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace UnitTests.Tests.Core.Services;

public class WebhookCatalogueServiceTests
{
    [Fact]
    public void Get_PublishesEverySubscribableEventPlusTheTestEvent()
    {
        // Arrange
        var sut = new WebhookCatalogueService(Options.Create(new AppConfig()));

        // Act
        var catalogue = sut.Get();

        // Assert
        catalogue.Events.Should().HaveCount(WebhookEvents.All.Count);
        catalogue.Events.Where(x => x.IsSubscribable).Select(x => x.Event.ToString())
            .Should().BeEquivalentTo(WebhookEvents.Subscribable);
        catalogue.Events.Single(x => !x.IsSubscribable).Event.ToString().Should().Be(WebhookEvents.Test);
    }

    [Fact]
    public void Get_DoesNotPublishTheStatusPageEvents()
    {
        // Arrange
        var sut = new WebhookCatalogueService(Options.Create(new AppConfig()));

        // Act
        var catalogue = sut.Get();

        // Assert
        catalogue.Events.Select(x => x.Event.ToString())
            .Should().NotContain([WebhookEvents.IncidentUpdated, WebhookEvents.MaintenanceScheduled]);
    }

    [Fact]
    public void Get_GivenConfiguredRanges_PublishesThemForAllowlisting()
    {
        // Arrange
        var sut = new WebhookCatalogueService(Options.Create(new AppConfig
        {
            WebhookConfig = new WebhookConfig { SourceIpRanges = ["203.0.113.0/24", "2001:db8::/32"] }
        }));

        // Act
        var catalogue = sut.Get();

        // Assert
        catalogue.SourceIpRanges.Select(x => x.ToString())
            .Should().BeEquivalentTo("203.0.113.0/24", "2001:db8::/32");
    }

    [Fact]
    public void Get_GivenNoConfiguredRanges_PublishesAnEmptyListRatherThanAPlaceholder()
    {
        // Arrange
        var sut = new WebhookCatalogueService(Options.Create(new AppConfig()));

        // Act
        var catalogue = sut.Get();

        // Assert
        catalogue.SourceIpRanges.Should().BeEmpty();
    }

    [Fact]
    public void Get_PublishesTheCurrentVersionAmongTheSupportedOnes()
    {
        // Arrange
        var sut = new WebhookCatalogueService(Options.Create(new AppConfig()));

        // Act
        var catalogue = sut.Get();

        // Assert
        ((int)catalogue.CurrentPayloadVersion).Should().Be(WebhookPayloadVersions.Current);
        catalogue.SupportedPayloadVersions.Select(x => (int)x).Should().Contain(WebhookPayloadVersions.Current);
    }
}
