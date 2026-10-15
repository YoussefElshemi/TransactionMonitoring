using System.Text.Json;
using Core.Enums;
using FluentValidation.TestHelper;
using Presentation.Models.Rules;
using Presentation.Validators.Rules;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Presentation.Validators.Rules;

public class RuleExpressionValidatorTests : BaseTestClass
{
    private readonly RuleExpressionValidator _sut = new();

    [Fact]
    public void Validate_FieldExpression_SucceedsValidation()
    {
        // Arrange
        var expression = new RuleExpressionDto { Field = "card.bin" };

        // Act
        var result = _sut.TestValidate(expression);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_FunctionOverAField_SucceedsValidation()
    {
        // Arrange
        var expression = Function(RuleFunction.DaysSince, new RuleExpressionDto { Field = "account.openedAt" });

        // Act
        var result = _sut.TestValidate(expression);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NestedArithmetic_SucceedsValidation()
    {
        // Arrange
        var expression = Function(
            RuleFunction.Divide,
            Function(RuleFunction.Sum, new RuleExpressionDto { Field = "parties[*].amount" }),
            new RuleExpressionDto { Value = 2 });

        // Act
        var result = _sut.TestValidate(expression);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ExpressionThatIsNoneOfTheThreeForms_FailsValidation()
    {
        // Arrange
        var expression = new RuleExpressionDto();

        // Act
        var result = _sut.TestValidate(expression);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("An expression must be a field, a party, a velocity window, a value or a function, and only one of the five.");
    }

    [Fact]
    public void Validate_ExpressionThatIsBothAFieldAndAFunction_FailsValidation()
    {
        // Arrange
        var expression = Function(RuleFunction.Abs, new RuleExpressionDto { Field = "amount" }) with { Field = "amount" };

        // Act
        var result = _sut.TestValidate(expression);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("An expression must be a field, a party, a velocity window, a value or a function, and only one of the five.");
    }

    [Fact]
    public void Validate_UnreadableFieldPath_FailsValidation()
    {
        // Arrange
        var expression = new RuleExpressionDto { Field = "parties[" };

        // Act
        var result = _sut.TestValidate(expression);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("'parties[' is not a readable field path.");
    }

    [Fact]
    public void Validate_FunctionGivenTheWrongNumberOfArguments_FailsValidation()
    {
        // Arrange
        var expression = Function(RuleFunction.Add, new RuleExpressionDto { Field = "amount" });

        // Act
        var result = _sut.TestValidate(expression);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("'Add' takes 2 argument(s) but was given 1.");
    }

    [Fact]
    public void Validate_AggregateOverAPathWithoutAWildcard_FailsValidation()
    {
        // Arrange
        var expression = Function(RuleFunction.Sum, new RuleExpressionDto { Field = "amount" });

        // Act
        var result = _sut.TestValidate(expression);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("'Sum' reduces many values, so its argument must be a field path carrying '[*]', a party role or a velocity window.");
    }

    [Fact]
    public void Validate_NonAggregateOverAWildcardPath_FailsValidation()
    {
        // Arrange
        var expression = Function(RuleFunction.Abs, new RuleExpressionDto { Field = "parties[*].amount" });

        // Act
        var result = _sut.TestValidate(expression);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("'Abs' reads a single value, so its argument must not be a field path carrying '[*]'.");
    }

    [Fact]
    public void Validate_ExpressionNestedDeeperThanTheEngineComputes_FailsValidation()
    {
        // Arrange
        var expression = new RuleExpressionDto { Field = "amount" };

        for (var depth = 0; depth < 6; depth++)
        {
            expression = Function(RuleFunction.Abs, expression);
        }

        // Act
        var result = _sut.TestValidate(expression);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("An expression cannot be nested more than 5 deep.");
    }

    [Theory]
    [InlineData("{\"currency\":\"GBP\"}")]
    [InlineData("[1, 2]")]
    public void Validate_ValueExpressionThatIsNotASingleValue_FailsValidation(string json)
    {
        // Arrange
        var expression = new RuleExpressionDto { Value = JsonSerializer.Deserialize<JsonElement>(json) };

        // Act
        var result = _sut.TestValidate(expression);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("A value expression must be a single string, number or boolean.");
    }

    private static RuleExpressionDto Function(RuleFunction function, params RuleExpressionDto[] arguments)
    {
        return new RuleExpressionDto
        {
            Function = function.ToString(),
            Arguments = arguments
        };
    }
}
