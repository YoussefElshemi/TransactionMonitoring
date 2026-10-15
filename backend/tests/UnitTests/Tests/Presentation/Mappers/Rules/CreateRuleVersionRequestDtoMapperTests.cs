using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Mappers.Rules;

public class CreateRuleVersionRequestDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateRuleVersionRequestDto()
    {
        // Arrange
        var createRuleVersionRequestDto = FakeCreateRuleVersionRequestDto.CreateValid(Fixture);
        var createdByUserId = FakeApplicationUserId.CreateValid(Fixture);

        // Act
        var createRuleVersionRequest = createRuleVersionRequestDto.ToModel(createdByUserId);

        // Assert
        createRuleVersionRequest.RuleId.Should().Be(new RuleId(createRuleVersionRequestDto.RuleId));
        createRuleVersionRequest.RuleBody.Should().BeEquivalentTo(createRuleVersionRequestDto.Body.RuleBody.ToModel());
        createRuleVersionRequest.CreatedByUserId.Should().Be(createdByUserId);
    }
}
