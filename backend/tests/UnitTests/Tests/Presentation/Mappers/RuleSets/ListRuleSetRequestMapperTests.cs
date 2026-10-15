using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.RuleSets;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

namespace UnitTests.Tests.Presentation.Mappers.RuleSets;

public class ListRuleSetRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListRuleSetRequestDto()
    {
        // Arrange
        var listRuleSetRequestDto = FakeListRuleSetRequestDto.CreateValid(Fixture);

        // Act
        var listRuleSetRequest = listRuleSetRequestDto.ToModel();

        // Assert
        listRuleSetRequest.OrganizationId.Should().Be(new OrganizationId(listRuleSetRequestDto.OrganizationId));
        listRuleSetRequest.FriendlyName.Should().Be(new FriendlyName(listRuleSetRequestDto.FriendlyName!));
        listRuleSetRequest.PageNumber.Should().Be(new PageNumber(listRuleSetRequestDto.PageNumber));
        listRuleSetRequest.PageSize.Should().Be(new PageSize(listRuleSetRequestDto.PageSize));
        listRuleSetRequest.FromDate.Should().Be(new DateRange(listRuleSetRequestDto.FromDate));
        listRuleSetRequest.ToDate.Should().Be(new DateRange(listRuleSetRequestDto.ToDate));
    }
}