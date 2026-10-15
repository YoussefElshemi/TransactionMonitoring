using System.Text.Json.Nodes;
using Core.Enums;
using Core.Helpers;
using Core.Models.Rules;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class RuleExpressionEvaluatorTests : BaseTestClass
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 13, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluate_GivenAField_ReadsThePayloadValue()
    {
        // Arrange
        var payload = new JsonObject { ["amount"] = 1200 };

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(Field("amount"), payload, Now);

        // Assert
        actual.Should().Be(1200L);
    }

    [Theory]
    [InlineData(RuleFunction.Add, 120)]
    [InlineData(RuleFunction.Subtract, 80)]
    [InlineData(RuleFunction.Multiply, 2000)]
    [InlineData(RuleFunction.Divide, 5)]
    [InlineData(RuleFunction.Modulo, 0)]
    public void Evaluate_GivenArithmetic_ComputesTheResult(RuleFunction function, int expected)
    {
        // Arrange
        var payload = new JsonObject { ["amount"] = 100, ["fee"] = 20 };
        var expression = Function(function, Field("amount"), Field("fee"));

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(expression, payload, Now);

        // Assert
        actual.Should().Be((decimal)expected);
    }

    [Fact]
    public void Evaluate_GivenADivisionByZero_ComputesNothing()
    {
        // Arrange
        var payload = new JsonObject { ["amount"] = 100, ["parts"] = 0 };
        var expression = Function(RuleFunction.Divide, Field("amount"), Field("parts"));

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(expression, payload, Now);

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void Evaluate_GivenAFieldThePayloadDoesNotCarry_ComputesNothing()
    {
        // Arrange
        var payload = new JsonObject { ["amount"] = 100 };
        var expression = Function(RuleFunction.Add, Field("amount"), Field("fee"));

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(expression, payload, Now);

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void Evaluate_GivenALiteralArgument_ComputesAgainstIt()
    {
        // Arrange
        var payload = new JsonObject { ["amount"] = 250 };
        var expression = Function(RuleFunction.Multiply, Field("amount"), Value(2));

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(expression, payload, Now);

        // Assert
        actual.Should().Be(500m);
    }

    [Fact]
    public void Evaluate_GivenLength_CountsTheCharacters()
    {
        // Arrange
        var payload = new JsonObject { ["reference"] = "ABC123" };

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(Function(RuleFunction.Length, Field("reference")), payload, Now);

        // Assert
        actual.Should().Be(6m);
    }

    [Theory]
    [InlineData(RuleFunction.Count, 3)]
    [InlineData(RuleFunction.Sum, 600)]
    [InlineData(RuleFunction.Min, 100)]
    [InlineData(RuleFunction.Max, 300)]
    [InlineData(RuleFunction.Average, 200)]
    public void Evaluate_GivenAnAggregateOverAWildcardPath_ReducesTheValues(RuleFunction function, int expected)
    {
        // Arrange
        var payload = new JsonObject
        {
            ["parties"] = new JsonArray(
                new JsonObject { ["amount"] = 100 },
                new JsonObject { ["amount"] = 200 },
                new JsonObject { ["amount"] = 300 })
        };

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(Function(function, Field("parties[*].amount")), payload, Now);

        // Assert
        actual.Should().Be((decimal)expected);
    }

    [Fact]
    public void Evaluate_GivenACountOverTheElementsThemselves_CountsEveryElement()
    {
        // Arrange
        var payload = new JsonObject
        {
            ["parties"] = new JsonArray(
                new JsonObject { ["amount"] = 100 },
                new JsonObject { ["amount"] = 200 },
                new JsonObject { ["country"] = "GB" })
        };

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(Function(RuleFunction.Count, Field("parties[*]")), payload, Now);

        // Assert
        actual.Should().Be(3m);
    }

    [Fact]
    public void Evaluate_GivenASumOverAnEmptyCollection_ComputesNothing()
    {
        // Arrange
        var payload = new JsonObject { ["parties"] = new JsonArray() };

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(Function(RuleFunction.Sum, Field("parties[*].amount")), payload, Now);

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void Evaluate_GivenACountOverAnEmptyCollection_ComputesZero()
    {
        // Arrange
        var payload = new JsonObject { ["parties"] = new JsonArray() };

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(Function(RuleFunction.Count, Field("parties[*].amount")), payload, Now);

        // Assert
        actual.Should().Be(0m);
    }

    [Fact]
    public void Evaluate_GivenHourOfDay_ReadsTheHourInTheOffsetThePayloadCarries()
    {
        // Arrange
        var payload = new JsonObject { ["occurredAt"] = "2026-08-16T02:15:00+02:00" };

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(Function(RuleFunction.HourOfDay, Field("occurredAt")), payload, Now);

        // Assert
        actual.Should().Be(2m);
    }

    [Fact]
    public void Evaluate_GivenMinuteOfDay_CountsFromMidnight()
    {
        // Arrange
        var payload = new JsonObject { ["occurredAt"] = "2026-08-16T02:15:00Z" };

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(Function(RuleFunction.MinuteOfDay, Field("occurredAt")), payload, Now);

        // Assert
        actual.Should().Be(135m);
    }

    [Theory]
    [InlineData("2026-08-17T09:00:00Z", 1)]
    [InlineData("2026-08-22T09:00:00Z", 6)]
    [InlineData("2026-08-16T09:00:00Z", 7)]
    public void Evaluate_GivenDayOfWeek_NumbersTheDayFromMonday(string occurredAt, int expected)
    {
        // Arrange
        var payload = new JsonObject { ["occurredAt"] = occurredAt };

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(Function(RuleFunction.DayOfWeek, Field("occurredAt")), payload, Now);

        // Assert
        actual.Should().Be((decimal)expected);
    }

    [Fact]
    public void Evaluate_GivenDaysSince_MeasuresAgainstTheEvaluationClock()
    {
        // Arrange
        var payload = new JsonObject { ["openedAt"] = "2026-07-17T13:30:00Z" };

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(Function(RuleFunction.DaysSince, Field("openedAt")), payload, Now);

        // Assert
        actual.Should().Be(30m);
    }

    [Fact]
    public void Evaluate_GivenHoursSince_MeasuresAgainstTheEvaluationClock()
    {
        // Arrange
        var payload = new JsonObject { ["openedAt"] = "2026-08-16T01:30:00Z" };

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(Function(RuleFunction.HoursSince, Field("openedAt")), payload, Now);

        // Assert
        actual.Should().Be(12m);
    }

    [Fact]
    public void Evaluate_GivenATemporalFunctionOverSomethingThatIsNotATimestamp_ComputesNothing()
    {
        // Arrange
        var payload = new JsonObject { ["openedAt"] = "not a timestamp" };

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(Function(RuleFunction.DaysSince, Field("openedAt")), payload, Now);

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void Evaluate_GivenATimestampWithoutAnOffset_ReadsItAsUtc()
    {
        // Arrange
        var payload = new JsonObject { ["openedAt"] = "2026-08-16T01:30:00" };

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(Function(RuleFunction.HoursSince, Field("openedAt")), payload, Now);

        // Assert
        actual.Should().Be(12m);
    }

    [Fact]
    public void Evaluate_GivenTheWrongNumberOfArguments_ComputesNothing()
    {
        // Arrange
        var payload = new JsonObject { ["amount"] = 100 };
        var expression = Function(RuleFunction.Add, Field("amount"));

        // Act
        var actual = RuleExpressionEvaluator.Evaluate(expression, payload, Now);

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void Describe_GivenANestedExpression_ReadsAsTheRuleWroteIt()
    {
        // Arrange
        var expression = Function(RuleFunction.Divide, Function(RuleFunction.Sum, Field("parties[*].amount")), Value(2));

        // Act
        var actual = RuleExpressionEvaluator.Describe(expression);

        // Assert
        actual.Should().Be("divide(sum(parties[*].amount), 2)");
    }

    private static RuleExpressionModel Field(string field)
    {
        return new RuleExpressionModel { Field = field };
    }

    private static RuleExpressionModel Value(object value)
    {
        return new RuleExpressionModel { Value = value };
    }

    private static RuleExpressionModel Function(RuleFunction function, params RuleExpressionModel[] arguments)
    {
        return new RuleExpressionModel { Function = function, Arguments = arguments };
    }
}
