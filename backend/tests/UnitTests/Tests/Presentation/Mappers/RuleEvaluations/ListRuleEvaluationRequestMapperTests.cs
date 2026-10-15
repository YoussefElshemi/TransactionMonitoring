using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.RuleEvaluations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleEvaluations;

namespace UnitTests.Tests.Presentation.Mappers.RuleEvaluations;

public class ListRuleEvaluationRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListRuleEvaluationRequestDto()
    {
        // Arrange
        var listRuleEvaluationRequestDto = FakeListRuleEvaluationRequestDto.CreateValid(Fixture);

        // Act
        var listRuleEvaluationRequest = listRuleEvaluationRequestDto.ToModel();

        // Assert
        listRuleEvaluationRequest.OrganizationId.Should().Be(new OrganizationId(listRuleEvaluationRequestDto.OrganizationId));
        listRuleEvaluationRequest.RuleId.Should().Be(new RuleId(listRuleEvaluationRequestDto.RuleId!.Value));
        listRuleEvaluationRequest.PageNumber.Should().Be(new PageNumber(listRuleEvaluationRequestDto.PageNumber));
        listRuleEvaluationRequest.PageSize.Should().Be(new PageSize(listRuleEvaluationRequestDto.PageSize));
        listRuleEvaluationRequest.FromDate.Should().Be(new DateRange(listRuleEvaluationRequestDto.FromDate));
        listRuleEvaluationRequest.ToDate.Should().Be(new DateRange(listRuleEvaluationRequestDto.ToDate));
    }
}
