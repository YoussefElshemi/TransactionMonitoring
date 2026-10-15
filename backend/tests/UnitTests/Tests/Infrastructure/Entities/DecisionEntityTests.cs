using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Entities;

public class DecisionEntityTests : BaseTestClass
{
    [Fact]
    public void GetHashCode_WithTheTransactionBackReferencePopulated_TerminatesOnIdentity()
    {
        // Arrange
        var decisionEntity = FakeDecisionEntity.CreateValid(Fixture);
        var transactionEntity = decisionEntity.Transaction with
        {
            Decision = decisionEntity
        };

        // Assert
        decisionEntity.GetHashCode().Should().Be(decisionEntity.DecisionId.GetHashCode());
        transactionEntity.GetHashCode().Should().Be(transactionEntity.GetHashCode());
    }

    [Fact]
    public void Equals_ComparesDecisionsOnIdentity()
    {
        // Arrange
        var decisionEntity = FakeDecisionEntity.CreateValid(Fixture);
        var sameDecision = decisionEntity with { StatusId = decisionEntity.StatusId + 1 };
        var otherDecision = FakeDecisionEntity.CreateValid(Fixture);

        // Assert
        decisionEntity.Equals(sameDecision).Should().BeTrue();
        decisionEntity.Equals(otherDecision).Should().BeFalse();
    }
}
