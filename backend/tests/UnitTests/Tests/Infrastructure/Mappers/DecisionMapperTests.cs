using AutoFixture;
using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Decisions;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class DecisionMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_Decision()
    {
        // Arrange
        var decisionEntity = FakeDecisionEntity.CreateValid(Fixture);
        var externalReference = FakeExternalReference.CreateValid(Fixture);

        // Act
        var decision = decisionEntity.ToModel(externalReference);

        // Assert
        decision.DecisionId.Should().Be(new DecisionId(decisionEntity.DecisionId));
        decision.OrganizationId.Should().Be(new OrganizationId(decisionEntity.OrganizationId));
        decision.TransactionId.Should().Be(new TransactionId(decisionEntity.TransactionId));
        decision.ExternalReference.Should().Be(externalReference);
        decision.Status.Should().Be((DecisionStatus)decisionEntity.StatusId);
        decision.ReasonCode.Should().Be((DecisionReasonCode)decisionEntity.ReasonCodeId);
        decision.HeldAt.Should().Be(new HeldAt(decisionEntity.HeldAt!.Value));
        decision.CreatedAt.Should().Be(new CreatedAt(decisionEntity.CreatedAt));
        decision.UpdatedAt.Should().Be(new UpdatedAt(decisionEntity.UpdatedAt));
    }

    [Fact]
    public void ToModel_Decision_WithoutATransactionReference()
    {
        // Arrange
        var decisionEntity = FakeDecisionEntity.CreateValid(Fixture);

        // Act
        var decision = decisionEntity.ToModel(null);

        // Assert
        decision.ExternalReference.Should().BeNull();
    }

    [Fact]
    public void ToModel_Decision_TakesItsFiredRulesFromItsOwnLinks()
    {
        // Arrange
        var ruleEvaluationEntity = FakeRuleEvaluationEntity.CreateValid(Fixture);
        var decisionEntity = FakeDecisionEntity.CreateValid(Fixture) with
        {
            RuleEvaluations = [ruleEvaluationEntity]
        };

        // Act
        var decision = decisionEntity.ToModel(null);

        // Assert
        decision.RuleEvaluations.Should().ContainSingle();
        decision.RuleEvaluations.Single().RuleEvaluationId.Should().Be(new RuleEvaluationId(ruleEvaluationEntity.RuleEvaluationId));
    }

    [Fact]
    public void ToModel_Decision_IgnoresEvaluationsFromOtherRoundsOnTheSameTransaction()
    {
        // Arrange
        var ruleEvaluationEntity = FakeRuleEvaluationEntity.CreateValid(Fixture);
        var decisionEntity = FakeDecisionEntity.CreateValid(Fixture) with
        {
            RuleEvaluations = [ruleEvaluationEntity]
        };

        decisionEntity.Transaction.RuleEvaluations.Add(FakeRuleEvaluationEntity.CreateValid(Fixture));

        // Act
        var decision = decisionEntity.ToModel(null);

        // Assert
        decision.RuleEvaluations.Should().ContainSingle();
        decision.RuleEvaluations.Single().RuleEvaluationId.Should().Be(new RuleEvaluationId(ruleEvaluationEntity.RuleEvaluationId));
    }

    [Fact]
    public void ToModel_Decision_ResolvedByAUser()
    {
        // Arrange
        var applicationUserEntity = FakeApplicationUserEntity.CreateValid(Fixture);
        var decisionEntity = FakeDecisionEntity.CreateValid(Fixture) with
        {
            StatusId = (int)DecisionStatus.Released,
            ReasonCodeId = (int)DecisionReasonCode.AnalystApproved,
            ResolvedByUserId = applicationUserEntity.Id,
            ResolvedByUser = applicationUserEntity,
            ResolvedAt = Fixture.Create<DateTimeOffset>()
        };

        // Act
        var decision = decisionEntity.ToModel(null);

        // Assert
        decision.ResolvedByUserId.Should().Be(new ApplicationUserId(applicationUserEntity.Id));
        decision.ResolvedByUser.Should().NotBeNull();
        decision.ResolvedAt.Should().Be(new ResolvedAt(decisionEntity.ResolvedAt!.Value));
    }

    [Fact]
    public void ToEntity_Decision()
    {
        // Arrange
        var decision = FakeDecision.CreateValid(Fixture);

        // Act
        var decisionEntity = decision.ToEntity();

        // Assert
        decisionEntity.DecisionId.Should().Be(decision.DecisionId);
        decisionEntity.OrganizationId.Should().Be(decision.OrganizationId);
        decisionEntity.TransactionId.Should().Be(decision.TransactionId);
        decisionEntity.StatusId.Should().Be((int)decision.Status);
        decisionEntity.ReasonCodeId.Should().Be((int)decision.ReasonCode);
        decisionEntity.CaseId.Should().Be(decision.CaseId!.Value);
        decisionEntity.ResolvedByUserId.Should().Be(decision.ResolvedByUserId!.Value);
        decisionEntity.HeldAt.Should().Be(decision.HeldAt!.Value);
        decisionEntity.CreatedAt.Should().Be(decision.CreatedAt);
        decisionEntity.UpdatedAt.Should().Be(decision.UpdatedAt);
    }

    [Fact]
    public void ToEntity_Decision_WithoutACaseOrResolvingUser()
    {
        // Arrange
        var decision = FakeDecision.CreateValid(Fixture) with
        {
            CaseId = null,
            ResolvedByUserId = null,
            ResolvedByUser = null,
            HeldAt = null,
            ResolvedAt = null
        };

        // Act
        var decisionEntity = decision.ToEntity();

        // Assert
        decisionEntity.CaseId.Should().BeNull();
        decisionEntity.ResolvedByUserId.Should().BeNull();
        decisionEntity.HeldAt.Should().BeNull();
        decisionEntity.ResolvedAt.Should().BeNull();
    }
}
