using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.RuleSets;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

namespace UnitTests.Tests.Presentation.Mappers.RuleSets;

public class UpdateRuleSetRequestDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_UpdateRuleSetRequestDto()
    {
        // Arrange
        var updateRuleSetRequestDto = FakeUpdateRuleSetRequestDto.CreateValid(Fixture);

        // Act
        var updateRuleSetRequest = updateRuleSetRequestDto.ToModel();

        // Assert
        updateRuleSetRequest.RuleSetId.Should().Be(new RuleSetId(updateRuleSetRequestDto.RuleSetId));
        updateRuleSetRequest.FriendlyName.Should().Be(new FriendlyName(updateRuleSetRequestDto.Body.FriendlyName!));
        updateRuleSetRequest.IsEnabled.Should().Be(new IsEnabled(updateRuleSetRequestDto.Body.IsEnabled!.Value));
    }
}