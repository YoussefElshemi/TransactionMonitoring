using Core.Enums;
using Core.Mappers;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Mappers;

public class CreateRuleVersionRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToVersion_SnapshotsRuleBodyFromRequestAndMetadataFromRule()
    {
        // Arrange
        var rule = FakeRule.CreateValid(Fixture);
        var ruleVersionId = FakeRuleVersionId.CreateValid(Fixture);
        var versionNumber = new VersionNumber(5);
        var request = new CreateRuleVersionRequest
        {
            RuleId = rule.RuleId,
            RuleBody = FakeRuleBody.CreateValid(Fixture),
            Action = RuleAction.Block,
            Severity = RuleSeverity.High,
            RiskWeight = new RiskWeight(40),
            CreatedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };

        // Act
        var ruleVersion = request.ToVersion(rule, ruleVersionId, versionNumber, MockTimeProvider, Mode.Live);

        // Assert
        ruleVersion.RuleVersionId.Should().Be(ruleVersionId);
        ruleVersion.RuleId.Should().Be(rule.RuleId);
        ruleVersion.VersionNumber.Should().Be(versionNumber);
        ruleVersion.RuleBody.Should().Be(request.RuleBody);
        ruleVersion.RuleSetId.Should().Be(rule.RuleSetId);
        ruleVersion.OrganizationId.Should().Be(rule.OrganizationId);
        ruleVersion.CreatedByUserId.Should().Be(request.CreatedByUserId);
        ruleVersion.CreatedAt.Should().Be(new CreatedAt(MockTimeProvider.GetUtcNow()));
    }
}
