using AutoFixture;
using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Alerts;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class AlertMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_Alert()
    {
        // Arrange
        var alertEntity = FakeAlertEntity.CreateValid(Fixture);

        // Act
        var alert = alertEntity.ToModel();

        // Assert
        alert.AlertId.Should().Be(new AlertId(alertEntity.AlertId));
        alert.FriendlyName.Should().Be(new FriendlyName(alertEntity.FriendlyName));
        alert.OrganizationId.Should().Be(new OrganizationId(alertEntity.OrganizationId));
        alert.Status.Should().Be((AlertStatus)alertEntity.StatusId);
        alert.Severity.Should().Be((RuleSeverity)alertEntity.SeverityId);
        alert.RuleId.Should().Be(new RuleId(alertEntity.RuleId));
        alert.RuleVersionId.Should().Be(new RuleVersionId(alertEntity.RuleVersionId));
        alert.RuleEvaluationId.Should().Be(new RuleEvaluationId(alertEntity.RuleEvaluationId));
        alert.TransactionId.Should().Be(new TransactionId(alertEntity.TransactionId));
        alert.PartyId.Should().Be(new PartyId(alertEntity.PartyId!.Value));
        alert.PartyRole.Should().Be((PartyRole?)alertEntity.PartyRoleId);
        alert.RaisedAt.Should().Be(new RaisedAt(alertEntity.RaisedAt));
        alert.CreatedAt.Should().Be(new CreatedAt(alertEntity.CreatedAt));
        alert.UpdatedAt.Should().Be(new UpdatedAt(alertEntity.UpdatedAt));
    }

    [Fact]
    public void ToModel_AlertWithoutASubject()
    {
        // Arrange
        var alertEntity = FakeAlertEntity.CreateValid(Fixture) with
        {
            PartyId = null,
            PartyRoleId = null
        };

        // Act
        var alert = alertEntity.ToModel();

        // Assert
        alert.PartyId.Should().BeNull();
        alert.PartyRole.Should().BeNull();
    }

    [Fact]
    public void ToModel_SuppressedAlert()
    {
        // Arrange
        var suppressedByAlertId = Fixture.Create<Guid>();
        var caseId = Fixture.Create<Guid>();
        var closedAt = Fixture.Create<DateTimeOffset>();

        var alertEntity = FakeAlertEntity.CreateValid(Fixture) with
        {
            StatusId = (int)AlertStatus.Suppressed,
            SuppressionReasonId = (int)AlertSuppressionReason.DuplicateInWindow,
            SuppressedByAlertId = suppressedByAlertId,
            CaseId = caseId,
            ClosedAt = closedAt
        };

        // Act
        var alert = alertEntity.ToModel();

        // Assert
        alert.Status.Should().Be(AlertStatus.Suppressed);
        alert.SuppressionReason.Should().Be(AlertSuppressionReason.DuplicateInWindow);
        alert.SuppressedByAlertId.Should().Be(new AlertId(suppressedByAlertId));
        alert.CaseId.Should().Be(new CaseId(caseId));
        alert.ClosedAt.Should().Be(new ClosedAt(closedAt));
    }

    [Fact]
    public void ToModel_AlertWithoutADeletedAt()
    {
        // Arrange
        var alertEntity = FakeAlertEntity.CreateValid(Fixture) with
        {
            IsDeleted = false,
            DeletedAt = null
        };

        // Act
        var alert = alertEntity.ToModel();

        // Assert
        alert.IsDeleted.Should().Be(new IsDeleted(false));
        alert.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void ToEntity_Alert()
    {
        // Arrange
        var alert = FakeAlert.CreateValid(Fixture);

        // Act
        var alertEntity = alert.ToEntity();

        // Assert
        alertEntity.AlertId.Should().Be(alert.AlertId);
        alertEntity.FriendlyName.Should().Be((string)alert.FriendlyName);
        alertEntity.OrganizationId.Should().Be(alert.OrganizationId);
        alertEntity.StatusId.Should().Be((int)alert.Status);
        alertEntity.SeverityId.Should().Be((int)alert.Severity);
        alertEntity.RuleId.Should().Be(alert.RuleId);
        alertEntity.RuleVersionId.Should().Be(alert.RuleVersionId);
        alertEntity.RuleEvaluationId.Should().Be(alert.RuleEvaluationId);
        alertEntity.TransactionId.Should().Be(alert.TransactionId);
        alertEntity.PartyId.Should().Be(alert.PartyId);
        alertEntity.PartyRoleId.Should().Be((int?)alert.PartyRole);
        alertEntity.RaisedAt.Should().Be(alert.RaisedAt);
    }

    [Fact]
    public void ToEntity_SuppressedAlert()
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
        var alertEntity = alert.ToEntity();

        // Assert
        alertEntity.StatusId.Should().Be((int)AlertStatus.Suppressed);
        alertEntity.SuppressionReasonId.Should().Be((int)AlertSuppressionReason.DuplicateInWindow);
        alertEntity.SuppressedByAlertId.Should().Be((Guid)suppressedByAlertId);
        alertEntity.CaseId.Should().Be((Guid)caseId);
        alertEntity.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void ToEntity_AlertWithoutASubjectOrCase()
    {
        // Arrange
        var alert = FakeAlert.CreateValid(Fixture) with
        {
            PartyId = null,
            PartyRole = null,
            CaseId = null
        };

        // Act
        var alertEntity = alert.ToEntity();

        // Assert
        alertEntity.PartyId.Should().BeNull();
        alertEntity.PartyRoleId.Should().BeNull();
        alertEntity.CaseId.Should().BeNull();
    }

    [Fact]
    public void ToEntity_ThenToModel_RoundTrips()
    {
        // Arrange
        var alert = FakeAlert.CreateValid(Fixture) with
        {
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };

        // Act
        var roundTripped = alert.ToEntity().ToModel();

        // Assert
        roundTripped.Should().BeEquivalentTo(alert, options => options.Excluding(x => x.RuleEvaluation));
    }
}
