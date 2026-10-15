using Core.Enums;
using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Mappers;

public class CreateRuleRequestMapperMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateRuleRequestMapper()
    {
        // Arrange
        var createRuleRequest = FakeCreateRuleRequest.CreateValid(Fixture);
        var currentRuleVersionId = FakeRuleVersionId.CreateValid(Fixture);

        // Act
        var rule = createRuleRequest.ToModel(currentRuleVersionId, MockTimeProvider, Mode.Live);

        // Assert
        rule.CurrentRuleVersionId.Should().Be(currentRuleVersionId);
        rule.FriendlyName.Should().Be(createRuleRequest.FriendlyName);
        rule.RuleBody.Should().Be(createRuleRequest.RuleBody);
        rule.OrganizationId.Should().Be(createRuleRequest.OrganizationId);
        rule.Organization.Should().BeNull();
        rule.IsEnabled.Should().Be(createRuleRequest.IsEnabled);
        rule.RuleSetId.Should().Be(createRuleRequest.RuleSetId);
        rule.CreatedByUserId.Should().Be(createRuleRequest.CreatedByUserId);
        rule.HasActiveDeployment.Should().Be(new HasActiveDeployment(false));
        rule.IsDeleted.Should().Be(new IsDeleted(false));
        rule.DeletedAt.Should().BeNull();
    }
}