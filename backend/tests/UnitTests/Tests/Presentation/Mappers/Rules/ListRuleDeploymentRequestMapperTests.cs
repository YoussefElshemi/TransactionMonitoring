using AutoFixture;
using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Mappers.Rules;

public class ListRuleDeploymentRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListRuleDeploymentRequestDto()
    {
        // Arrange
        var ruleId = new RuleId(Fixture.Create<Guid>());
        var dto = FakeListRuleDeploymentRequestDto.CreateValid(Fixture);

        // Act
        var request = dto.ToModel(ruleId);

        // Assert
        request.OrganizationId.Should().Be(new OrganizationId(dto.OrganizationId));
        request.RuleId.Should().Be(ruleId);
        request.PageNumber.Should().Be(new PageNumber(dto.PageNumber));
        request.PageSize.Should().Be(new PageSize(dto.PageSize));
        request.FromDate.Should().Be(new DateRange(dto.FromDate));
        request.ToDate.Should().Be(new DateRange(dto.ToDate));
    }
}
