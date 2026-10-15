using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Cases;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

namespace UnitTests.Tests.Presentation.Mappers.Cases;

public class ListCaseRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListCaseRequest()
    {
        // Arrange
        var listCaseRequestDto = FakeListCaseRequestDto.CreateValid(Fixture);

        // Act
        var listCaseRequest = listCaseRequestDto.ToModel();

        // Assert
        listCaseRequest.OrganizationId.Should().Be(new OrganizationId(listCaseRequestDto.OrganizationId));
        listCaseRequest.FriendlyName.Should().Be(new FriendlyName(listCaseRequestDto.FriendlyName!));
        listCaseRequest.Status.Should().Be(Enum.Parse<CaseStatus>(listCaseRequestDto.Status!, true));
        listCaseRequest.Severity.Should().Be(Enum.Parse<RuleSeverity>(listCaseRequestDto.Severity!, true));
        listCaseRequest.AssignedToUserId.Should().Be(new ApplicationUserId(listCaseRequestDto.AssignedToUserId!.Value));
        listCaseRequest.PageNumber.Should().Be(new PageNumber(listCaseRequestDto.PageNumber));
        listCaseRequest.PageSize.Should().Be(new PageSize(listCaseRequestDto.PageSize));
        listCaseRequest.FromDate.Should().Be(new DateRange(listCaseRequestDto.FromDate));
        listCaseRequest.ToDate.Should().Be(new DateRange(listCaseRequestDto.ToDate));
    }
}