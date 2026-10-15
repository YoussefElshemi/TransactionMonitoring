using Core.Constants;
using Core.ValueObjects;
using FluentAssertions;

namespace UnitTests.Tests.Core.ValueObjects;

public class WebhookValueObjectTests
{
    [Fact]
    public void WebhookEvent_GivenAPublishedName_IsAccepted()
    {
        // Arrange
        var name = WebhookEvents.CaseCreated;

        // Act
        var webhookEvent = new WebhookEvent(name);

        // Assert
        webhookEvent.ToString().Should().Be(name);
    }

    [Theory]
    [InlineData("case.create")]
    [InlineData("Case.Created")]
    [InlineData("incident.updated")]
    public void WebhookEvent_GivenAnUnpublishedName_Throws(string name)
    {
        // Act
        var act = () => new WebhookEvent(name);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WebhookEvents_TestIsDeliverableButNotSubscribable()
    {
        // Assert
        WebhookEvents.All.Should().Contain(WebhookEvents.Test);
        WebhookEvents.Subscribable.Should().NotContain(WebhookEvents.Test);
    }

    [Fact]
    public void WebhookPayloadVersion_GivenAnUnsupportedVersion_Throws()
    {
        // Act
        var act = () => new WebhookPayloadVersion(99);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WebhookPayloadVersion_GivenTheCurrentVersion_IsAccepted()
    {
        // Act
        var version = new WebhookPayloadVersion(WebhookPayloadVersions.Current);

        // Assert
        ((int)version).Should().Be(WebhookPayloadVersions.Current);
    }

    [Theory]
    [InlineData("203.0.113.0/24")]
    [InlineData("2001:db8::/32")]
    public void SourceIpRange_GivenACidrRange_IsAccepted(string range)
    {
        // Act
        var sourceIpRange = new SourceIpRange(range);

        // Assert
        sourceIpRange.ToString().Should().Be(range);
    }

    [Theory]
    [InlineData("203.0.113.0")]
    [InlineData("not-a-range")]
    [InlineData("203.0.113.0/99")]
    public void SourceIpRange_GivenSomethingThatIsNotACidrRange_Throws(string range)
    {
        // Act
        var act = () => new SourceIpRange(range);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ConsecutiveFailures_GivenANegativeCount_Throws()
    {
        // Act
        var act = () => new ConsecutiveFailures(-1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
