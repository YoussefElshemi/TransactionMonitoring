using Core.Enums;
using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleSets;

namespace UnitTests.Tests.Core.Mappers;

public class CreateRuleSetRequestMapperMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateRuleSetRequestMapper()
    {
        // Arrange
        var createRuleSetRequest = FakeCreateRuleSetRequest.CreateValid(Fixture);

        // Act
        var rule = createRuleSetRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        rule.FriendlyName.Should().Be(createRuleSetRequest.FriendlyName);
        rule.OrganizationId.Should().Be(createRuleSetRequest.OrganizationId);
        rule.IsEnabled.Should().Be(createRuleSetRequest.IsEnabled);
        rule.CreatedByUserId.Should().Be(createRuleSetRequest.CreatedByUserId);
        rule.IsDeleted.Should().Be(new IsDeleted(false));
        rule.DeletedAt.Should().BeNull();
    }
}