using Core.Enums;
using Core.Mappers.Webhooks;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Alerts;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Mappers.Webhooks;

public class AlertWebhookMessageMapperTests : BaseTestClass
{
    [Fact]
    public void ToWebhook_Alert()
    {
        // Arrange
        var alert = FakeAlert.CreateValid(Fixture);

        // Act
        var alertWebhookMessage = alert.ToWebhook();

        // Assert
        alertWebhookMessage.AlertId.Should().Be(alert.AlertId);
        alertWebhookMessage.FriendlyName.Should().Be((string)alert.FriendlyName);
        alertWebhookMessage.Status.Should().Be(alert.Status.ToString());
        alertWebhookMessage.Severity.Should().Be(alert.Severity.ToString());
        alertWebhookMessage.OrganizationId.Should().Be(alert.OrganizationId);
        alertWebhookMessage.RuleId.Should().Be(alert.RuleId);
        alertWebhookMessage.TransactionId.Should().Be(alert.TransactionId);
        alertWebhookMessage.PartyId.Should().Be(alert.PartyId);
        alertWebhookMessage.PartyRole.Should().Be(alert.PartyRole?.ToString());
        alertWebhookMessage.RaisedAt.Should().Be(alert.RaisedAt);
    }

    [Fact]
    public void ToWebhook_SuppressedAlert_CarriesWhatItFoldedInto()
    {
        // Arrange
        var suppressedByAlertId = FakeAlertId.CreateValid(Fixture);
        var caseId = FakeCaseId.CreateValid(Fixture);

        var alert = FakeAlert.CreateValid(Fixture) with
        {
            Status = AlertStatus.Suppressed,
            SuppressionReason = AlertSuppressionReason.DuplicateInWindow,
            SuppressedByAlertId = suppressedByAlertId,
            CaseId = caseId,
            ClosedAt = FakeClosedAt.CreateValid(Fixture)
        };

        // Act
        var alertWebhookMessage = alert.ToWebhook();

        // Assert
        alertWebhookMessage.Status.Should().Be(nameof(AlertStatus.Suppressed));
        alertWebhookMessage.SuppressionReason.Should().Be(nameof(AlertSuppressionReason.DuplicateInWindow));
        alertWebhookMessage.SuppressedByAlertId.Should().Be((Guid)suppressedByAlertId);
        alertWebhookMessage.CaseId.Should().Be((Guid)caseId);
        alertWebhookMessage.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void ToWebhook_OpenAlertWithoutASubject()
    {
        // Arrange
        var alert = FakeAlert.CreateValid(Fixture) with
        {
            Status = AlertStatus.Open,
            PartyId = null,
            PartyRole = null
        };

        // Act
        var alertWebhookMessage = alert.ToWebhook();

        // Assert
        alertWebhookMessage.PartyId.Should().BeNull();
        alertWebhookMessage.PartyRole.Should().BeNull();
        alertWebhookMessage.SuppressionReason.Should().BeNull();
        alertWebhookMessage.SuppressedByAlertId.Should().BeNull();
        alertWebhookMessage.ClosedAt.Should().BeNull();
    }
}
