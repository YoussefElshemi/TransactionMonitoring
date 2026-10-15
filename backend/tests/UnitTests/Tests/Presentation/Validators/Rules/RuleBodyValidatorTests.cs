using System.Text.Json;
using Core.Enums;
using FluentValidation.TestHelper;
using Presentation.Models.Rules;
using Presentation.Validators.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Validators.Rules;

public class RuleBodyValidatorTests : BaseTestClass
{
    private readonly RuleBodyValidator _sut = new();

    [Fact]
    public void Validate_BasicRuleWithValidFields_SucceedsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture);

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_BasicRuleWithoutField_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with { Field = null };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Rule must either be a basic rule (Condition with a Field or an Expression, and a Value) or one of And, Or, Not, but not both.");
    }

    [Fact]
    public void Validate_BasicRuleWithNullCondition_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with { Condition = null };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Rule must either be a basic rule (Condition with a Field or an Expression, and a Value) or one of And, Or, Not, but not both.");
    }

    [Fact]
    public void Validate_BasicRuleWithConditionIsNullButValueProvided_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Condition = RuleCondition.IsNull.ToString(),
            Value = "NotAllowedValue"
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Value must be not set when the condition is 'IsNull'.");
    }

    [Fact]
    public void Validate_CompositeRuleWithValidAnd_SucceedsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateCompositeRule(Fixture) with
        {
            And = [FakeRuleBodyDto.CreateBasicRule(Fixture)]
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_CompositeRuleWithEmptyAnd_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateCompositeRule(Fixture) with { And = [] };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.And)
            .WithErrorMessage("The 'And' collection must contain a rule.");
    }

    [Fact]
    public void Validate_CompositeRuleExceedingMaxAndRules_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateCompositeRule(Fixture) with
        {
            And = Enumerable.Range(0, 15).Select(_ => FakeRuleBodyDto.CreateBasicRule(Fixture)).ToArray()
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.And)
            .WithErrorMessage($"The 'And' collection must not contain more than {RuleBodyValidator.MaxRulesInCollection} rules.");
    }

    [Fact]
    public void Validate_CompositeRuleExceedingMaxDepth_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateCompositeRule(Fixture) with
        {
            And =
            [
                new RuleBodyDto
                {
                    And =
                    [
                        new RuleBodyDto
                        {
                            And =
                            [
                                new RuleBodyDto
                                {
                                    And =
                                    [
                                        new RuleBodyDto
                                        {
                                            And =
                                            [
                                                new RuleBodyDto
                                                {
                                                    And =
                                                    [
                                                        new RuleBodyDto
                                                        {
                                                            And =
                                                            [
                                                                new RuleBodyDto
                                                                {
                                                                    And =
                                                                    [
                                                                        new RuleBodyDto
                                                                        {
                                                                            And =
                                                                            [
                                                                                new RuleBodyDto
                                                                                {
                                                                                    And =
                                                                                    [
                                                                                        new RuleBodyDto
                                                                                        {
                                                                                            And = [FakeRuleBodyDto.CreateBasicRule(Fixture)]
                                                                                        }
                                                                                    ]
                                                                                }
                                                                            ]
                                                                        }
                                                                    ]
                                                                }
                                                            ]
                                                        }
                                                    ]
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage($"Rule depth cannot be greater than {RuleBodyValidator.MaxDepth}");
    }

    [Fact]
    public void Validate_CompositeRuleWithInvalidOrRules_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateCompositeRule(Fixture) with
        {
            Or =
            [
                FakeRuleBodyDto.CreateBasicRule(Fixture) with { Field = null } // Invalid rule
            ]
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor("Or[0]")
            .WithErrorMessage("Rule must either be a basic rule (Condition with a Field or an Expression, and a Value) or one of And, Or, Not, but not both.");
    }

    [Fact]
    public void Validate_NotRuleWithValidSingleElement_SucceedsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateCompositeRule(Fixture) with
        {
            Not = [FakeRuleBodyDto.CreateBasicRule(Fixture)]
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NotRuleWithMultipleElements_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateCompositeRule(Fixture) with
        {
            Not =
            [
                FakeRuleBodyDto.CreateBasicRule(Fixture),
                FakeRuleBodyDto.CreateBasicRule(Fixture)
            ]
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Not)
            .WithErrorMessage("The 'Not' collection must contain only one element.");
    }

    [Fact]
    public void Validate_EmptyRule_FailsValidation()
    {
        // Arrange
        var rule = new RuleBodyDto();

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Rule must either be a basic rule (Condition with a Field or an Expression, and a Value) or one of And, Or, Not, but not both.");
    }

    [Theory]
    [InlineData(RuleCondition.StartsWith, "ValidString")]
    [InlineData(RuleCondition.Contains, "ValidString")]
    [InlineData(RuleCondition.EndsWith, "ValidString")]
    public void Validate_BasicRuleWithValidStringConditions_SucceedsValidation(RuleCondition condition, string value)
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Condition = condition.ToString(),
            Value = JsonSerializer.SerializeToElement(value)
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(nameof(RuleCondition.In), new[] { 1, 2, 3 })]
    public void Validate_BasicRuleWithValidArrayCondition_SucceedsValidation(string condition, object value)
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Condition = condition,
            Value = JsonSerializer.SerializeToElement(value)
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_BasicRuleWithADottedFieldPath_SucceedsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with { Field = "card.bin" };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("parties[")]
    [InlineData("card..bin")]
    [InlineData("parties[a].country")]
    public void Validate_BasicRuleWithAFieldThatIsNotAPath_SucceedsValidation(string field)
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with { Field = field };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_BasicRuleWithAMistypedWildcardPath_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with { Field = "parties[*.country" };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("'parties[*.country' is not a readable field path.");
    }

    [Fact]
    public void Validate_BasicRuleWithAWildcardAndAQuantifier_SucceedsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Field = "parties[*].country",
            Quantifier = RuleQuantifier.Any.ToString()
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_BasicRuleWithAWildcardButNoQuantifier_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with { Field = "parties[*].country" };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("A field path carrying '[*]' must state a Quantifier of 'Any' or 'All'.");
    }

    [Fact]
    public void Validate_BasicRuleWithAQuantifierButNoWildcard_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Field = "country",
            Quantifier = RuleQuantifier.All.ToString()
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("A Quantifier can only be used on a field path carrying '[*]'.");
    }

    [Fact]
    public void Validate_BasicRuleWithAnExpression_SucceedsValidation()
    {
        // Arrange
        var rule = new RuleBodyDto
        {
            Condition = RuleCondition.LessThan.ToString(),
            Expression = new RuleExpressionDto
            {
                Function = RuleFunction.DaysSince.ToString(),
                Arguments = [new RuleExpressionDto { Field = "account.openedAt" }]
            },
            Value = 30
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_BasicRuleWithBothAFieldAndAnExpression_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Field = "amount",
            Expression = new RuleExpressionDto { Field = "amount" }
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("A rule must address exactly one of a Field, an Expression, a Party or a Velocity window.");
    }

    [Fact]
    public void Validate_BasicRuleWithAQuantifierOnAnExpression_FailsValidation()
    {
        // Arrange
        var rule = new RuleBodyDto
        {
            Condition = RuleCondition.LessThan.ToString(),
            Quantifier = RuleQuantifier.Any.ToString(),
            Expression = new RuleExpressionDto { Field = "amount" },
            Value = 30
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("A quantifier can only be used on a field path, not on an expression.");
    }

    [Theory]
    [InlineData(RuleCondition.GreaterThan)]
    [InlineData(RuleCondition.LessThanEqual)]
    public void Validate_BasicRuleOrderingAgainstAString_SucceedsValidation(RuleCondition condition)
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Condition = condition.ToString(),
            Field = "occurredAt",
            Value = "2026-01-01T00:00:00Z"
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_BasicRuleOrderingAgainstABoolean_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Condition = RuleCondition.GreaterThan.ToString(),
            Value = true
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Value must be a number or a string when the condition is 'GreaterThan'.");
    }

    [Fact]
    public void Validate_BasicRuleWithNotIn_SucceedsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Condition = RuleCondition.NotIn.ToString(),
            Value = new object[] { "GB", "IE" }
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_BasicRuleWithNotInAgainstASingleValue_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Condition = RuleCondition.NotIn.ToString(),
            Value = "GB"
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Value must be a list when the condition is 'NotIn'.");
    }

    [Fact]
    public void Validate_BasicRuleWithAnEmptyInList_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Condition = RuleCondition.In.ToString(),
            Value = Array.Empty<object>()
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Value must not be an empty list when the condition is 'In'.");
    }

    [Fact]
    public void Validate_BasicRuleWithBetween_SucceedsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Condition = RuleCondition.Between.ToString(),
            Value = new object[] { 500, 9000 }
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_BasicRuleWithBetweenAndOneBound_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Condition = RuleCondition.Between.ToString(),
            Value = new object[] { 500 }
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Value must be a list of exactly two bounds when the condition is 'Between'.");
    }

    [Fact]
    public void Validate_BasicRuleWithReversedBetweenBounds_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Condition = RuleCondition.Between.ToString(),
            Value = new object[] { 9000, 500 }
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("The 'Between' bounds must be given lower first.");
    }

    [Fact]
    public void Validate_BasicRuleWithMixedBetweenBounds_FailsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Condition = RuleCondition.Between.ToString(),
            Value = new object[] { 500, "9000" }
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("The 'Between' bounds must be two numbers or two strings.");
    }

    [Fact]
    public void Validate_BasicRuleWithAPatternTheEngineCanRun_SucceedsValidation()
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Condition = RuleCondition.Matches.ToString(),
            Value = "^GB-[0-9]{4}$"
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("GB-[0-9")]
    [InlineData(@"(\w+)\1")]
    public void Validate_BasicRuleWithAPatternTheEngineCannotRun_FailsValidation(string pattern)
    {
        // Arrange
        var rule = FakeRuleBodyDto.CreateBasicRule(Fixture) with
        {
            Condition = RuleCondition.Matches.ToString(),
            Value = pattern
        };

        // Act
        var result = _sut.TestValidate(rule);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Value must be a regular expression the engine can run when the condition is 'Matches'.");
    }
}
