using Core.Extensions;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class RuleVersionMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_RuleVersion()
    {
        // Arrange
        var ruleVersionEntity = FakeRuleVersionEntity.CreateValid(Fixture);

        // Act
        var ruleVersion = ruleVersionEntity.ToModel();

        // Assert
        ruleVersion.RuleVersionId.Should().Be(new RuleVersionId(ruleVersionEntity.RuleVersionId));
        ruleVersion.RuleId.Should().Be(new RuleId(ruleVersionEntity.RuleId));
        ruleVersion.VersionNumber.Should().Be(new VersionNumber(ruleVersionEntity.VersionNumber));
        ruleVersion.RuleBody.Should().BeEquivalentTo(ruleVersionEntity.RuleBody.FromBase64String().TryDeserialize<RuleBodyModel>().ToModel());
        ruleVersion.RuleSetId.Should().Be(new RuleSetId(ruleVersionEntity.RuleSetId));
        ruleVersion.OrganizationId.Should().Be(new OrganizationId(ruleVersionEntity.OrganizationId));
        ruleVersion.CreatedByUserId.Should().Be(new ApplicationUserId(ruleVersionEntity.CreatedByUserId));
        ruleVersion.CreatedAt.Should().Be(new CreatedAt(ruleVersionEntity.CreatedAt));
    }

    [Fact]
    public void ToEntity_RuleVersion()
    {
        // Arrange
        var ruleVersion = FakeRuleVersion.CreateValid(Fixture);

        // Act
        var ruleVersionEntity = ruleVersion.ToEntity();

        // Assert
        ruleVersionEntity.RuleVersionId.Should().Be(ruleVersion.RuleVersionId);
        ruleVersionEntity.RuleId.Should().Be(ruleVersion.RuleId);
        ruleVersionEntity.VersionNumber.Should().Be(ruleVersion.VersionNumber);
        ruleVersionEntity.RuleBody.Should().Be(ruleVersion.RuleBody.ToBase64String());
        ruleVersionEntity.RuleSetId.Should().Be(ruleVersion.RuleSetId);
        ruleVersionEntity.OrganizationId.Should().Be(ruleVersion.OrganizationId);
        ruleVersionEntity.CreatedByUserId.Should().Be(ruleVersion.CreatedByUserId);
        ruleVersionEntity.CreatedAt.Should().Be(ruleVersion.CreatedAt);
    }
}
