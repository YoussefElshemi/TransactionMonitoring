using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Mappers.Rules;

public class ListRuleRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListRuleRequestDto()
    {
        // Arrange
        var listRuleRequestDto = FakeListRuleRequestDto.CreateValid(Fixture);

        // Act
        var listRuleRequest = listRuleRequestDto.ToModel();

        // Assert
        listRuleRequest.OrganizationId.Should().Be(new OrganizationId(listRuleRequestDto.OrganizationId));
        listRuleRequest.RuleSetId.Should().Be(new RuleSetId(listRuleRequestDto.RuleSetId));
        listRuleRequest.FriendlyName.Should().Be(new FriendlyName(listRuleRequestDto.FriendlyName!));
        listRuleRequest.PageNumber.Should().Be(new PageNumber(listRuleRequestDto.PageNumber));
        listRuleRequest.PageSize.Should().Be(new PageSize(listRuleRequestDto.PageSize));
        listRuleRequest.FromDate.Should().Be(new DateRange(listRuleRequestDto.FromDate));
        listRuleRequest.ToDate.Should().Be(new DateRange(listRuleRequestDto.ToDate));
    }
}