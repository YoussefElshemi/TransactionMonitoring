using AutoFixture;
using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Alerts;
using Presentation.Models.Alerts;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Presentation.Mappers.Alerts;

public class ListAlertRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListAlertRequest()
    {
        // Arrange
        var organizationId = Fixture.Create<Guid>();
        var ruleId = Fixture.Create<Guid>();
        var partyId = Fixture.Create<Guid>();
        var caseId = Fixture.Create<Guid>();

        var listAlertRequestDto = new ListAlertRequestDto
        {
            OrganizationId = organizationId,
            FriendlyName = "ALT-1042",
            Status = nameof(AlertStatus.Open),
            Severity = nameof(RuleSeverity.Critical),
            RuleId = ruleId,
            PartyId = partyId,
            CaseId = caseId,
            PageNumber = 3,
            PageSize = 25
        };

        // Act
        var listAlertRequest = listAlertRequestDto.ToModel();

        // Assert
        listAlertRequest.OrganizationId.Should().Be(new OrganizationId(organizationId));
        listAlertRequest.FriendlyName.Should().Be(new FriendlyName("ALT-1042"));
        listAlertRequest.Status.Should().Be(AlertStatus.Open);
        listAlertRequest.Severity.Should().Be(RuleSeverity.Critical);
        listAlertRequest.RuleId.Should().Be(new RuleId(ruleId));
        listAlertRequest.PartyId.Should().Be(new PartyId(partyId));
        listAlertRequest.CaseId.Should().Be(new CaseId(caseId));
        listAlertRequest.PageNumber.Should().Be(new PageNumber(3));
        listAlertRequest.PageSize.Should().Be(new PageSize(25));
    }

    [Fact]
    public void ToModel_NoFilters_LeavesThemAbsent()
    {
        // Arrange
        var listAlertRequestDto = new ListAlertRequestDto
        {
            OrganizationId = Fixture.Create<Guid>()
        };

        // Act
        var listAlertRequest = listAlertRequestDto.ToModel();

        // Assert
        listAlertRequest.FriendlyName.Should().BeNull();
        listAlertRequest.Status.Should().BeNull();
        listAlertRequest.Severity.Should().BeNull();
        listAlertRequest.RuleId.Should().BeNull();
        listAlertRequest.PartyId.Should().BeNull();
        listAlertRequest.CaseId.Should().BeNull();
    }

    [Fact]
    public void ToModel_DefaultsToTheFirstPage()
    {
        // Arrange
        var listAlertRequestDto = new ListAlertRequestDto
        {
            OrganizationId = Fixture.Create<Guid>()
        };

        // Act
        var listAlertRequest = listAlertRequestDto.ToModel();

        // Assert
        listAlertRequest.PageNumber.Should().Be(new PageNumber(1));
        listAlertRequest.PageSize.Should().Be(new PageSize(10));
    }

    [Theory]
    [InlineData("open", AlertStatus.Open)]
    [InlineData("SUPPRESSED", AlertStatus.Suppressed)]
    [InlineData("Closed", AlertStatus.Closed)]
    public void ToModel_ParsesTheStatusRegardlessOfCase(string status, AlertStatus expected)
    {
        // Arrange
        var listAlertRequestDto = new ListAlertRequestDto
        {
            OrganizationId = Fixture.Create<Guid>(),
            Status = status
        };

        // Act
        var listAlertRequest = listAlertRequestDto.ToModel();

        // Assert
        listAlertRequest.Status.Should().Be(expected);
    }

    [Fact]
    public void ToModel_ListCaseAlertRequest_UsesTheCaseIdItWasGiven()
    {
        // Arrange
        var routeCaseId = Fixture.Create<Guid>();

        var listCaseAlertRequestDto = new ListCaseAlertRequestDto
        {
            OrganizationId = Fixture.Create<Guid>(),
            RouteCaseId = routeCaseId,
            CaseId = routeCaseId
        };

        // Act
        var listAlertRequest = listCaseAlertRequestDto.ToModel();

        // Assert
        listAlertRequest.CaseId.Should().Be(new CaseId(routeCaseId));
    }
}
