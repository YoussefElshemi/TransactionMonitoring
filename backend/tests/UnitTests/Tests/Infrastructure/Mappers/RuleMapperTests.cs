using Core.Extensions;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class RuleMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_Rule()
    {
        // Arrange
        var ruleEntity = FakeRuleEntity.CreateValid(Fixture);

        // Act
        var rule = ruleEntity.ToModel();

        // Assert
        rule.RuleId.Should().Be(new RuleId(ruleEntity.RuleId));
        rule.HasActiveDeployment.Should().Be(new HasActiveDeployment(ruleEntity.HasActiveDeployment));
        rule.FriendlyName.Should().Be(new FriendlyName(ruleEntity.FriendlyName));
        rule.RuleBody.Should().BeEquivalentTo(ruleEntity.RuleBody.FromBase64String().TryDeserialize<RuleBodyModel>().ToModel());
        rule.IsEnabled.Should().Be(new IsEnabled(ruleEntity.IsEnabled));
        rule.RuleSetId.Should().Be(new RuleSetId(ruleEntity.RuleSetId));
        rule.CreatedByUserId.Should().Be(new ApplicationUserId(ruleEntity.CreatedByUserId));
        rule.OrganizationId.Should().Be(new OrganizationId(ruleEntity.OrganizationId));
        rule.Organization.Should().BeEquivalentTo(ruleEntity.Organization.ToModel());
        rule.CreatedAt.Should().Be(new CreatedAt(ruleEntity.CreatedAt));
        rule.UpdatedAt.Should().Be(new UpdatedAt(ruleEntity.UpdatedAt));
        rule.IsDeleted.Should().Be(new IsDeleted(ruleEntity.IsDeleted));
        rule.DeletedAt.Should().Be(new DeletedAt(ruleEntity.DeletedAt!.Value));
    }

    [Fact]
    public void ToEntity_Rule()
    {
        // Arrange
        var rule = FakeRule.CreateValid(Fixture);

        // Act
        var ruleEntity = rule.ToEntity();

        // Assert
        ruleEntity.RuleId.Should().Be(rule.RuleId);
        ruleEntity.HasActiveDeployment.Should().Be(rule.HasActiveDeployment);
        ruleEntity.FriendlyName.Should().Be(rule.FriendlyName);
        ruleEntity.RuleBody.Should().Be(rule.RuleBody.ToBase64String());
        ruleEntity.IsEnabled.Should().Be(rule.IsEnabled);
        ruleEntity.RuleSetId.Should().Be(rule.RuleSetId);
        ruleEntity.CreatedByUserId.Should().Be(rule.CreatedByUserId);
        ruleEntity.OrganizationId.Should().Be(rule.OrganizationId);
        ruleEntity.CreatedAt.Should().Be(rule.CreatedAt);
        ruleEntity.UpdatedAt.Should().Be(rule.UpdatedAt);
        ruleEntity.IsDeleted.Should().Be(rule.IsDeleted);
        ruleEntity.DeletedAt.Should().Be(rule.DeletedAt!.Value);
    }
}