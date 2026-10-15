using Core.Extensions;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleEvaluations;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class RuleEvaluationMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_RuleEvaluation()
    {
        // Arrange
        var ruleEvaluationEntity = FakeRuleEvaluationEntity.CreateValid(Fixture);

        // Act
        var ruleEvaluation = ruleEvaluationEntity.ToModel();

        // Assert
        ruleEvaluation.RuleEvaluationId.Should().Be(new RuleEvaluationId(ruleEvaluationEntity.RuleEvaluationId));
        ruleEvaluation.OrganizationId.Should().Be(new OrganizationId(ruleEvaluationEntity.OrganizationId));
        ruleEvaluation.RuleId.Should().Be(new RuleId(ruleEvaluationEntity.RuleId));
        ruleEvaluation.Rule.Should().BeEquivalentTo(ruleEvaluationEntity.Rule.ToModel());
        ruleEvaluation.RuleVersionId.Should().Be(new RuleVersionId(ruleEvaluationEntity.RuleVersionId));
        ruleEvaluation.RuleVersion.Should().BeEquivalentTo(ruleEvaluationEntity.RuleVersion.ToModel());
        ruleEvaluation.TransactionId.Should().Be(new TransactionId(ruleEvaluationEntity.TransactionId));
        ruleEvaluation.Matched.Should().Be(new Matched(ruleEvaluationEntity.Matched));
        ruleEvaluation.ConditionMatches!.Serialize().Should().Be(ruleEvaluationEntity.ConditionMatches!.FromBase64String());
        ruleEvaluation.CreatedAt.Should().Be(new CreatedAt(ruleEvaluationEntity.CreatedAt));
        ruleEvaluation.UpdatedAt.Should().Be(new UpdatedAt(ruleEvaluationEntity.UpdatedAt));
        ruleEvaluation.IsDeleted.Should().Be(new IsDeleted(ruleEvaluationEntity.IsDeleted));
        ruleEvaluation.DeletedAt.Should().Be(new DeletedAt(ruleEvaluationEntity.DeletedAt!.Value));
    }

    [Fact]
    public void ToEntity_RuleEvaluation()
    {
        // Arrange
        var ruleEvaluation = FakeRuleEvaluation.CreateValid(Fixture);

        // Act
        var ruleEvaluationEntity = ruleEvaluation.ToEntity();

        // Assert
        ruleEvaluationEntity.RuleEvaluationId.Should().Be(ruleEvaluation.RuleEvaluationId);
        ruleEvaluationEntity.OrganizationId.Should().Be(ruleEvaluation.OrganizationId);
        ruleEvaluationEntity.RuleId.Should().Be(ruleEvaluation.RuleId);
        ruleEvaluationEntity.TransactionId.Should().Be(ruleEvaluation.TransactionId);
        ruleEvaluationEntity.Matched.Should().Be(ruleEvaluation.Matched);
        ruleEvaluationEntity.ConditionMatches!.FromBase64String().Should().Be(ruleEvaluation.ConditionMatches!.Serialize());
        ruleEvaluationEntity.CreatedAt.Should().Be(ruleEvaluation.CreatedAt);
        ruleEvaluationEntity.UpdatedAt.Should().Be(ruleEvaluation.UpdatedAt);
        ruleEvaluationEntity.IsDeleted.Should().Be(ruleEvaluation.IsDeleted);
        ruleEvaluationEntity.DeletedAt.Should().Be(ruleEvaluation.DeletedAt!.Value);
    }

    [Fact]
    public void ToModel_RuleEvaluation_WithoutConditionMatches()
    {
        // Arrange
        var ruleEvaluationEntity = FakeRuleEvaluationEntity.CreateValid(Fixture) with
        {
            ConditionMatches = null
        };

        // Act
        var ruleEvaluation = ruleEvaluationEntity.ToModel();

        // Assert
        ruleEvaluation.ConditionMatches.Should().BeNull();
    }

    [Fact]
    public void ToEntity_RuleEvaluation_WithoutConditionMatches()
    {
        // Arrange
        var ruleEvaluation = FakeRuleEvaluation.CreateValid(Fixture) with
        {
            ConditionMatches = null
        };

        // Act
        var ruleEvaluationEntity = ruleEvaluation.ToEntity();

        // Assert
        ruleEvaluationEntity.ConditionMatches.Should().BeNull();
    }
}
