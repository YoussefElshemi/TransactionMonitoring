using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Alerts;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Alerts;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Presentation.Mappers.Alerts;

public class AlertDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_Alert()
    {
        // Arrange
        var alert = FakeAlert.CreateValid(Fixture);

        // Act
        var alertResponseDto = alert.ToDto();

        // Assert
        alertResponseDto.AlertId.Should().Be(alert.AlertId);
        alertResponseDto.FriendlyName.Should().Be((string)alert.FriendlyName);
        alertResponseDto.OrganizationId.Should().Be(alert.OrganizationId);
        alertResponseDto.RuleId.Should().Be(alert.RuleId);
        alertResponseDto.RuleVersionId.Should().Be(alert.RuleVersionId);
        alertResponseDto.RuleEvaluationId.Should().Be(alert.RuleEvaluationId);
        alertResponseDto.TransactionId.Should().Be(alert.TransactionId);
        alertResponseDto.PartyId.Should().Be(alert.PartyId);
    }

    [Theory]
    [InlineData(AlertStatus.Open)]
    [InlineData(AlertStatus.Suppressed)]
    [InlineData(AlertStatus.Closed)]
    public void ToDto_WritesTheStatusName(AlertStatus status)
    {
        // Arrange
        var alert = FakeAlert.CreateValid(Fixture) with { Status = status };

        // Act
        var alertResponseDto = alert.ToDto();

        // Assert
        alertResponseDto.Status.Should().Be(status.ToString());
    }

    [Theory]
    [InlineData(RuleSeverity.Low)]
    [InlineData(RuleSeverity.Medium)]
    [InlineData(RuleSeverity.High)]
    [InlineData(RuleSeverity.Critical)]
    public void ToDto_WritesTheSeverityName(RuleSeverity severity)
    {
        // Arrange
        var alert = FakeAlert.CreateValid(Fixture) with { Severity = severity };

        // Act
        var alertResponseDto = alert.ToDto();

        // Assert
        alertResponseDto.Severity.Should().Be(severity.ToString());
    }

    [Fact]
    public void ToDto_SuppressedAlert_CarriesWhatItFoldedInto()
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
        var alertResponseDto = alert.ToDto();

        // Assert
        alertResponseDto.SuppressionReason.Should().Be(nameof(AlertSuppressionReason.DuplicateInWindow));
        alertResponseDto.SuppressedByAlertId.Should().Be((Guid)suppressedByAlertId);
        alertResponseDto.CaseId.Should().Be((Guid)caseId);
        alertResponseDto.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void ToDto_OpenAlert_WritesNoSuppressionFields()
    {
        // Arrange
        var alert = FakeAlert.CreateValid(Fixture) with { Status = AlertStatus.Open };

        // Act
        var alertResponseDto = alert.ToDto();

        // Assert
        alertResponseDto.SuppressionReason.Should().BeNull();
        alertResponseDto.SuppressedByAlertId.Should().BeNull();
        alertResponseDto.ClosedAt.Should().BeNull();
    }

    [Fact]
    public void ToDto_AlertWithoutASubject()
    {
        // Arrange
        var alert = FakeAlert.CreateValid(Fixture) with
        {
            PartyId = null,
            PartyRole = null
        };

        // Act
        var alertResponseDto = alert.ToDto();

        // Assert
        alertResponseDto.PartyId.Should().BeNull();
        alertResponseDto.PartyRole.Should().BeNull();
    }

    [Fact]
    public void ToDto_KeepsRaisedAtSeparateFromCreatedAt()
    {
        // Arrange
        var raisedAt = new RaisedAt(DateTimeOffset.UtcNow.AddDays(-30));
        var alert = FakeAlert.CreateValid(Fixture) with { RaisedAt = raisedAt };

        // Act
        var alertResponseDto = alert.ToDto();

        // Assert
        alertResponseDto.RaisedAt.Should().Be(raisedAt);
        alertResponseDto.CreatedAt.Should().Be(alert.CreatedAt);
        alertResponseDto.RaisedAt.Should().NotBe(alertResponseDto.CreatedAt);
    }
}
