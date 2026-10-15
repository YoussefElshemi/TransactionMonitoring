using Core.Enums;
using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Transactions;

namespace UnitTests.Tests.Core.Mappers;

public class CreateTransactionRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateTransactionRequest()
    {
        // Arrange
        var createTransactionRequest = FakeCreateTransactionRequest.CreateValid(Fixture);

        // Act
        var transaction = createTransactionRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        transaction.TransactionId.Should().NotBeNull();
        transaction.OrganizationId.Should().Be(createTransactionRequest.OrganizationId);
        transaction.ExternalReference.Should().Be(createTransactionRequest.ExternalReference);
        transaction.Payload.ToJsonString().Should().Be(createTransactionRequest.Payload.ToJsonString());
        transaction.RuleEvaluations.Should().BeEmpty();
        transaction.Cases.Should().BeEmpty();
        transaction.IsDeleted.Should().Be(new IsDeleted(false));
        transaction.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void ToModel_CreateTransactionRequest_LeavesTheFriendlyNameForTheService()
    {
        // Arrange
        var createTransactionRequest = FakeCreateTransactionRequest.CreateValid(Fixture);

        // Act
        var transaction = createTransactionRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        transaction.FriendlyName.Should().Be(default(FriendlyName));
    }

    [Fact]
    public void ToModel_CreateTransactionRequest_WithoutAnExternalReference()
    {
        // Arrange
        var createTransactionRequest = FakeCreateTransactionRequest.CreateValid(Fixture) with
        {
            ExternalReference = null
        };

        // Act
        var transaction = createTransactionRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        transaction.ExternalReference.Should().BeNull();
    }
}
