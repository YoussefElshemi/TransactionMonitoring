using System.Net;
using Core.Exceptions;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Exceptions;

public class TransactionPayloadMismatchExceptionTests : BaseTestClass
{
    [Fact]
    public void GetStatusCode_IsBadRequest()
    {
        // Arrange
        var exception = new TransactionPayloadMismatchException(FakeExternalReference.CreateValid(Fixture));

        // Act
        var statusCode = exception.GetStatusCode();

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void Message_NamesTheReferenceAndSaysWhatToDo()
    {
        // Arrange
        var externalReference = FakeExternalReference.CreateValid(Fixture);

        // Act
        var exception = new TransactionPayloadMismatchException(externalReference);

        // Assert
        exception.Message.Should().Contain(externalReference.ToString());
        exception.Message.Should().Contain("different payload");
        exception.GetTitle().Should().Be("Transaction reference does not match its payload");
    }

    [Fact]
    public void GetExtensions_PublishesTheReferenceAsAString()
    {
        // Arrange
        var externalReference = FakeExternalReference.CreateValid(Fixture);
        var exception = new TransactionPayloadMismatchException(externalReference);

        // Act
        var extensions = exception.GetExtensions();

        // Assert
        extensions.Should().NotBeNull();

        var property = extensions!.GetType().GetProperty("TransactionReference");
        property.Should().NotBeNull();
        property!.GetValue(extensions).Should().Be(externalReference.ToString());
    }

    [Fact]
    public void IsADomainException()
    {
        // Arrange
        var exception = new TransactionPayloadMismatchException(FakeExternalReference.CreateValid(Fixture));

        // Act & Assert
        exception.Should().BeAssignableTo<DomainException>();
    }
}
