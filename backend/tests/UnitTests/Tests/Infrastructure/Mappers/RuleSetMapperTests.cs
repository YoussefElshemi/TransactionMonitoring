using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleSets;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class RuleSetMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_RuleSet()
    {
        // Arrange
        var ruleSetEntity = FakeRuleSetEntity.CreateValid(Fixture);

        // Act
        var ruleSet = ruleSetEntity.ToModel(0);

        // Assert
        ruleSet.RuleSetId.Should().Be(new RuleSetId(ruleSetEntity.RuleSetId));
        ruleSet.FriendlyName.Should().Be(new FriendlyName(ruleSetEntity.FriendlyName));
        ruleSet.IsEnabled.Should().Be(new IsEnabled(ruleSetEntity.IsEnabled));
        ruleSet.CreatedByUserId.Should().Be(new ApplicationUserId(ruleSetEntity.CreatedByUserId));
        ruleSet.OrganizationId.Should().Be(new OrganizationId(ruleSetEntity.OrganizationId));
        ruleSet.CreatedAt.Should().Be(new CreatedAt(ruleSetEntity.CreatedAt));
        ruleSet.UpdatedAt.Should().Be(new UpdatedAt(ruleSetEntity.UpdatedAt));
        ruleSet.IsDeleted.Should().Be(new IsDeleted(ruleSetEntity.IsDeleted));
        ruleSet.DeletedAt.Should().Be(new DeletedAt(ruleSetEntity.DeletedAt!.Value));
    }

    [Fact]
    public void ToEntity_RuleSet()
    {
        // Arrange
        var ruleSet = FakeRuleSet.CreateValid(Fixture);

        // Act
        var ruleSetEntity = ruleSet.ToEntity();

        // Assert
        ruleSetEntity.RuleSetId.Should().Be(ruleSet.RuleSetId);
        ruleSetEntity.FriendlyName.Should().Be(ruleSet.FriendlyName);
        ruleSetEntity.IsEnabled.Should().Be(ruleSet.IsEnabled);
        ruleSetEntity.CreatedByUserId.Should().Be(ruleSet.CreatedByUserId);
        ruleSetEntity.OrganizationId.Should().Be(ruleSet.OrganizationId);
        ruleSetEntity.CreatedAt.Should().Be(ruleSet.CreatedAt);
        ruleSetEntity.UpdatedAt.Should().Be(ruleSet.UpdatedAt);
        ruleSetEntity.IsDeleted.Should().Be(ruleSet.IsDeleted);
        ruleSetEntity.DeletedAt.Should().Be(ruleSet.DeletedAt!.Value);
    }
}