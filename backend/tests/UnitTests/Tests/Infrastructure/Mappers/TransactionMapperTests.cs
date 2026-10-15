using Core.Extensions;
using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Transactions;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class TransactionMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_Transaction()
    {
        // Arrange
        var transactionEntity = FakeTransactionEntity.CreateValid(Fixture);

        // Act
        var transaction = transactionEntity.ToModel(null);

        // Assert
        transaction.TransactionId.Should().Be(new TransactionId(transactionEntity.TransactionId));
        transaction.OrganizationId.Should().Be(new OrganizationId(transactionEntity.OrganizationId));
        transaction.FriendlyName.Should().Be(new FriendlyName(transactionEntity.FriendlyName));
        transaction.ExternalReference.Should().Be(new ExternalReference(transactionEntity.ExternalReference!));
        transaction.Payload.Serialize().Should().Be(transactionEntity.Payload.FromBase64String());
        transaction.CreatedAt.Should().Be(new CreatedAt(transactionEntity.CreatedAt));
        transaction.UpdatedAt.Should().Be(new UpdatedAt(transactionEntity.UpdatedAt));
        transaction.IsDeleted.Should().Be(new IsDeleted(transactionEntity.IsDeleted));
        transaction.DeletedAt.Should().Be(new DeletedAt(transactionEntity.DeletedAt!.Value));
    }

    [Fact]
    public void ToModel_Transaction_WithoutAnExternalReference()
    {
        // Arrange
        var transactionEntity = FakeTransactionEntity.CreateValid(Fixture) with
        {
            ExternalReference = null
        };

        // Act
        var transaction = transactionEntity.ToModel(null);

        // Assert
        transaction.ExternalReference.Should().BeNull();
    }

    [Fact]
    public void ToModel_Transaction_WithoutADecision()
    {
        // Arrange
        var transactionEntity = FakeTransactionEntity.CreateValid(Fixture);

        // Act
        var transaction = transactionEntity.ToModel(null);

        // Assert
        transaction.Decision.Should().BeNull();
    }

    [Fact]
    public void ToModel_Transaction_CarriesItsDecisionUnderTheSameReference()
    {
        // Arrange
        var transactionEntity = FakeTransactionEntity.CreateValid(Fixture);
        transactionEntity = transactionEntity with
        {
            Decision = FakeDecisionEntity.CreateValid(Fixture) with
            {
                TransactionId = transactionEntity.TransactionId
            }
        };

        // Act
        var transaction = transactionEntity.ToModel(null);

        // Assert
        transaction.Decision.Should().NotBeNull();
        transaction.Decision!.DecisionId.Should().Be(new DecisionId(transactionEntity.Decision!.DecisionId));
        transaction.Decision.ExternalReference.Should().Be(transaction.ExternalReference);
    }

    [Fact]
    public void ToEntity_Transaction()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture);
        var dataKey = FakeDataKey.CreateValid(Fixture);

        // Act
        var transactionEntity = transaction.ToEntity(dataKey);

        // Assert
        transactionEntity.TransactionId.Should().Be(transaction.TransactionId);
        transactionEntity.OrganizationId.Should().Be(transaction.OrganizationId);
        transactionEntity.FriendlyName.Should().Be(transaction.FriendlyName);
        transactionEntity.ExternalReference.Should().Be(transaction.ExternalReference!.Value.ToString());

        transactionEntity.Payload.Should().NotContain(transaction.Payload.Serialize());
        transactionEntity.Payload.Should().StartWith(PayloadCipher.Version);
        transactionEntity.ToModel(dataKey).Payload.Serialize().Should().Be(transaction.Payload.Serialize());
        transactionEntity.CreatedAt.Should().Be(transaction.CreatedAt);
        transactionEntity.UpdatedAt.Should().Be(transaction.UpdatedAt);
        transactionEntity.IsDeleted.Should().Be(transaction.IsDeleted);
        transactionEntity.DeletedAt.Should().Be(transaction.DeletedAt!.Value);
    }

    [Fact]
    public void ToEntity_Transaction_WithoutAnExternalReference()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture) with
        {
            ExternalReference = null
        };

        // Act
        var transactionEntity = transaction.ToEntity(FakeDataKey.CreateValid(Fixture));

        // Assert
        transactionEntity.ExternalReference.Should().BeNull();
    }
}
