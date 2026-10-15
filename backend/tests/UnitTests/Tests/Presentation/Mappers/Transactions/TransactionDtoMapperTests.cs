using Core.Extensions;
using Core.Helpers;
using FluentAssertions;
using Presentation.Mappers.Cases;
using Presentation.Mappers.Decisions;
using Presentation.Mappers.RuleEvaluations;
using Presentation.Mappers.Transactions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Decisions;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleEvaluations;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Transactions;

namespace UnitTests.Tests.Presentation.Mappers.Transactions;

public class TransactionDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_Transaction()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture) with
        {
            RuleEvaluations = [FakeRuleEvaluation.CreateValid(Fixture)],
            Cases = [FakeCase.CreateValid(Fixture)]
        };

        // Act
        var transactionDto = transaction.ToDto();

        // Assert
        transactionDto.TransactionId.Should().Be(transaction.TransactionId);
        transactionDto.FriendlyName.Should().Be(transaction.FriendlyName);
        transactionDto.ExternalReference.Should().Be(transaction.ExternalReference!.Value.ToString());
        transactionDto.Payload.Serialize().Should().Be(PayloadMasker.Mask(transaction.Payload).Serialize());
        transactionDto.PayloadMasked.Should().BeTrue();
        transactionDto.RuleEvaluations.Should().BeEquivalentTo(transaction.RuleEvaluations.Select(x => x.ToDto()).ToArray());
        transactionDto.Cases.Should().BeEquivalentTo(transaction.Cases.Select(x => x.ToDto()).ToArray());
        transactionDto.CreatedAt.Should().Be(transaction.CreatedAt);
        transactionDto.UpdatedAt.Should().Be(transaction.UpdatedAt);
    }

    [Fact]
    public void ToDto_Transaction_CarriesItsDecision()
    {
        // Arrange
        var decision = FakeDecision.CreateValid(Fixture);
        var transaction = FakeTransaction.CreateValid(Fixture) with
        {
            Decision = decision
        };

        // Act
        var transactionDto = transaction.ToDto();

        // Assert
        transactionDto.Decision.Should().BeEquivalentTo(decision.ToDto());
    }

    [Fact]
    public void ToDto_Transaction_WithoutADecision()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture) with
        {
            Decision = null
        };

        // Act
        var transactionDto = transaction.ToDto();

        // Assert
        transactionDto.Decision.Should().BeNull();
    }

    [Fact]
    public void ToSummaryDto_Transaction()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture);

        // Act
        var transactionDto = transaction.ToSummaryDto();

        // Assert
        transactionDto.TransactionId.Should().Be(transaction.TransactionId);
        transactionDto.FriendlyName.Should().Be(transaction.FriendlyName);
        transactionDto.ExternalReference.Should().Be(transaction.ExternalReference!.Value.ToString());
        transactionDto.Payload.Serialize().Should().Be(PayloadMasker.Mask(transaction.Payload).Serialize());
        transactionDto.PayloadMasked.Should().BeTrue();
        transactionDto.CreatedAt.Should().Be(transaction.CreatedAt);
        transactionDto.UpdatedAt.Should().Be(transaction.UpdatedAt);
    }

    [Fact]
    public void ToSummaryDto_Transaction_WithoutAnExternalReference()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture) with
        {
            ExternalReference = null
        };

        // Act
        var transactionDto = transaction.ToSummaryDto();

        // Assert
        transactionDto.ExternalReference.Should().BeNull();
    }

    [Fact]
    public void ToDto_Transaction_WithoutAnExternalReference()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture) with
        {
            ExternalReference = null
        };

        // Act
        var transactionDto = transaction.ToDto();

        // Assert
        transactionDto.ExternalReference.Should().BeNull();
    }

    [Fact]
    public void ToRevealedDto_Transaction_HandsBackThePaymentAsItWasSent()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture);

        // Act
        var transactionDto = transaction.ToRevealedDto();

        // Assert
        transactionDto.Payload.Serialize().Should().Be(transaction.Payload.Serialize());
        transactionDto.PayloadMasked.Should().BeFalse();
    }

    [Fact]
    public void ToDto_Transaction_WithholdsWhatTheRevealedFormShows()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture);

        // Act
        var masked = transaction.ToDto();
        var revealed = transaction.ToRevealedDto();

        // Assert
        masked.Payload.Serialize().Should().NotBe(revealed.Payload.Serialize());
    }
}
