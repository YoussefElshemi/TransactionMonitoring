using Core.Exceptions;
using Core.Extensions;
using Core.Models.TransactionSchemas;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Exceptions;

public class PayloadSchemaViolationExceptionTests : BaseTestClass
{
    private static readonly List<SchemaViolation> Violations =
    [
        new()
        {
            Field = "instrument.number",
            Message = "must be one of 'card', 'bank' but the payload carries '4111111111111111'"
        }
    ];

    [Fact]
    public void Message_NamesTheFieldWithoutQuotingThePayment()
    {
        // Act
        var exception = new PayloadSchemaViolationException(Violations);

        // Assert
        exception.Message.Should().Contain("instrument.number");
        exception.Message.Should().NotContain("4111111111111111");
    }

    [Fact]
    public void GetExtensions_StillHandsTheFullDetailBackToTheCaller()
    {
        // Act
        var extensions = new PayloadSchemaViolationException(Violations).GetExtensions();

        // Assert
        extensions!.Serialize().Should().Contain("4111111111111111");
    }

    [Fact]
    public void Message_OnARuleViolationKeepsItsDetail()
    {
        // Arrange
        var violations = new List<SchemaViolation>
        {
            new() { Field = "amount", Message = "only allows 'GBP', 'EUR' but the rule compares it with 'USD'" }
        };

        // Act
        var exception = new RuleSchemaViolationException(violations);

        // Assert
        exception.Message.Should().Contain("USD");
    }
}
