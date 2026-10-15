using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.CaseRuleEvaluationLinks;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class CaseRuleEvaluationLinkMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CaseRuleLink()
    {
        // Arrange
        var caseRuleLinkEntity = FakeCaseRuleEvaluationLinkEntity.CreateValid(Fixture);

        // Act
        var caseRuleLink = caseRuleLinkEntity.ToModel();

        // Assert
        caseRuleLink.RuleEvaluationId.Should().Be(new RuleEvaluationId(caseRuleLinkEntity.RuleEvaluationId));
        caseRuleLink.CaseId.Should().Be(new CaseId(caseRuleLinkEntity.CaseId));
        caseRuleLink.CreatedAt.Should().Be(new CreatedAt(caseRuleLinkEntity.CreatedAt));
        caseRuleLink.UpdatedAt.Should().Be(new UpdatedAt(caseRuleLinkEntity.UpdatedAt));
    }

    [Fact]
    public void ToEntity_CaseRuleLink()
    {
        // Arrange
        var caseRuleLink = FakeCaseRuleEvaluationLink.CreateValid(Fixture);

        // Act
        var caseRuleLinkEntity = caseRuleLink.ToEntity();

        // Assert
        caseRuleLinkEntity.CaseId.Should().Be(caseRuleLink.CaseId);
        caseRuleLinkEntity.RuleEvaluationId.Should().Be(caseRuleLink.RuleEvaluationId);
        caseRuleLinkEntity.CreatedAt.Should().Be(caseRuleLink.CreatedAt);
        caseRuleLinkEntity.UpdatedAt.Should().Be(caseRuleLink.UpdatedAt);
    }
}