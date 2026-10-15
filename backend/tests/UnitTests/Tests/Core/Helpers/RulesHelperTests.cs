using System.Text.Json.Nodes;
using Core.Enums;
using Core.Helpers;
using Core.Models.Rules;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class RulesHelperTests : BaseTestClass
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluate_Int_IsNullCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.IsNull
        };

        var value = new JsonObject
        {
            ["Amount1"] = 5
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_Int_IsNullCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.IsNull
        };

        var value = new JsonObject
        {
            ["Amount"] = 7
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_Int_EqualToCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.EqualTo,
            Value = 5
        };

        var value = new JsonObject
        {
            ["Amount"] = 5
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_Int_EqualToCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.EqualTo,
            Value = 5
        };

        var value = new JsonObject
        {
            ["Amount"] = 7
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_Int_GreaterThanCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.GreaterThan,
            Value = 5
        };

        var value = new JsonObject
        {
            ["Amount"] = 6
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_Int_GreaterThanCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.GreaterThan,
            Value = 5
        };

        var value = new JsonObject
        {
            ["Amount"] = 4
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_Int_LessThanCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.LessThan,
            Value = 5
        };

        var value = new JsonObject
        {
            ["Amount"] = 4
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_Int_LessThanCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.LessThan,
            Value = 5
        };

        var value = new JsonObject
        {
            ["Amount"] = 6
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_Int_GreaterThanEqualCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.GreaterThanEqual,
            Value = 5
        };

        var valueGreater = new JsonObject
        {
            ["Amount"] = 6
        };

        var valueEqual = new JsonObject
        {
            ["Amount"] = 5
        };

        // Act
        var actualGreater = RuleHelper.Evaluate(rule, valueGreater, Now);
        var actualEqual = RuleHelper.Evaluate(rule, valueEqual, Now);

        // Assert
        Assert.True(actualGreater && actualEqual);
    }

    [Fact]
    public void Evaluate_Int_GreaterThanEqualCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.GreaterThanEqual,
            Value = 5
        };

        var value = new JsonObject
        {
            ["Amount"] = 4
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_Int_LessThanEqualCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.LessThanEqual,
            Value = 6
        };

        var valueLess = new JsonObject
        {
            ["Amount"] = 6
        };

        var valueEqual = new JsonObject
        {
            ["Amount"] = 5
        };

        // Act
        var actualLess = RuleHelper.Evaluate(rule, valueLess, Now);
        var actualEqual = RuleHelper.Evaluate(rule, valueEqual, Now);

        // Assert
        Assert.True(actualLess && actualEqual);
    }

    [Fact]
    public void Evaluate_Int_LessThanEqualCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.LessThanEqual,
            Value = 5
        };

        var value = new JsonObject
        {
            ["Amount"] = 6
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_Int_And_EqualToCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            And =
            [
                new RuleBodyModel
                {
                    Field = "Amount1",
                    Condition = RuleCondition.EqualTo,
                    Value = 5
                },
                new RuleBodyModel
                {
                    Field = "Amount2",
                    Condition = RuleCondition.EqualTo,
                    Value = 6
                }
            ]
        };

        var value = new JsonObject
        {
            ["Amount1"] = 5,
            ["Amount2"] = 6
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_Int_And_EqualToCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            And =
            [
                new RuleBodyModel
                {
                    Field = "Amount1",
                    Condition = RuleCondition.EqualTo,
                    Value = 5
                },
                new RuleBodyModel
                {
                    Field = "Amount2",
                    Condition = RuleCondition.EqualTo,
                    Value = 6
                }
            ]
        };

        var value = new JsonObject
        {
            ["Amount1"] = 6,
            ["Amount2"] = 6
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_Int_Not_EqualToCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Not =
            [
                new RuleBodyModel
                {
                    Field = "Amount",
                    Condition = RuleCondition.EqualTo,
                    Value = 6
                }
            ]
        };

        var value = new JsonObject
        {
            ["Amount"] = 5
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_Int_Not_EqualToCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Not =
            [
                new RuleBodyModel
                {
                    Field = "Amount",
                    Condition = RuleCondition.EqualTo,
                    Value = 6
                }
            ]
        };

        var value = new JsonObject
        {
            ["Amount"] = 6
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_Int_Or_EqualToCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Or =
            [
                new RuleBodyModel
                {
                    Field = "Amount",
                    Condition = RuleCondition.EqualTo,
                    Value = 5
                },
                new RuleBodyModel
                {
                    Field = "Amount",
                    Condition = RuleCondition.EqualTo,
                    Value = 6
                }
            ]
        };

        var value1 = new JsonObject
        {
            ["Amount"] = 5
        };

        var value2 = new JsonObject
        {
            ["Amount"] = 6
        };

        // Act
        var actual1 = RuleHelper.Evaluate(rule, value1, Now);
        var actual2 = RuleHelper.Evaluate(rule, value2, Now);

        // Assert
        Assert.True(actual1 && actual2);
    }

    [Fact]
    public void Evaluate_Int_Or_EqualToCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Or =
            [
                new RuleBodyModel
                {
                    Field = "Amount",
                    Condition = RuleCondition.EqualTo,
                    Value = 5
                },
                new RuleBodyModel
                {
                    Field = "Amount",
                    Condition = RuleCondition.EqualTo,
                    Value = 6
                }
            ]
        };

        var value = new JsonObject
        {
            ["Amount"] = 4
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_Int_Not_And_EqualToCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Not =
            [
                new RuleBodyModel
                {
                    And =
                    [
                        new RuleBodyModel
                        {
                            Field = "Amount1",
                            Condition = RuleCondition.EqualTo,
                            Value = 6
                        },
                        new RuleBodyModel
                        {
                            Field = "Amount2",
                            Condition = RuleCondition.EqualTo,
                            Value = 6
                        }
                    ]
                }
            ]
        };

        var value = new JsonObject
        {
            ["Amount1"] = 5,
            ["Amount2"] = 6
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_Int_Not_And_EqualToCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Not =
            [
                new RuleBodyModel
                {
                    And =
                    [
                        new RuleBodyModel
                        {
                            Field = "Amount1",
                            Condition = RuleCondition.EqualTo,
                            Value = 6
                        },
                        new RuleBodyModel
                        {
                            Field = "Amount2",
                            Condition = RuleCondition.EqualTo,
                            Value = 6
                        }
                    ]
                }
            ]
        };

        var value = new JsonObject
        {
            ["Amount1"] = 5,
            ["Amount2"] = 6
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_Int_And_Or_Or_EqualToCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            And =
            [
                new RuleBodyModel
                {
                    Or =
                    [
                        new RuleBodyModel
                        {
                            Field = "Amount1",
                            Condition = RuleCondition.EqualTo,
                            Value = 5
                        },
                        new RuleBodyModel
                        {
                            Field = "Amount1",
                            Condition = RuleCondition.EqualTo,
                            Value = 6
                        }
                    ]
                },
                new RuleBodyModel
                {
                    Or =
                    [
                        new RuleBodyModel
                        {
                            Field = "Amount2",
                            Condition = RuleCondition.EqualTo,
                            Value = 5
                        },
                        new RuleBodyModel
                        {
                            Field = "Amount2",
                            Condition = RuleCondition.EqualTo,
                            Value = 6
                        }
                    ]
                }
            ]
        };

        var value = new JsonObject
        {
            ["Amount1"] = 5,
            ["Amount2"] = 6
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_Int_And_Or_Or_EqualToCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            And =
            [
                new RuleBodyModel
                {
                    Or =
                    [
                        new RuleBodyModel
                        {
                            Field = "Amount1",
                            Condition = RuleCondition.EqualTo,
                            Value = 5
                        },
                        new RuleBodyModel
                        {
                            Field = "Amount1",
                            Condition = RuleCondition.EqualTo,
                            Value = 6
                        }
                    ]
                },
                new RuleBodyModel
                {
                    Or =
                    [
                        new RuleBodyModel
                        {
                            Field = "Amount2",
                            Condition = RuleCondition.EqualTo,
                            Value = 5
                        },
                        new RuleBodyModel
                        {
                            Field = "Amount2",
                            Condition = RuleCondition.EqualTo,
                            Value = 6
                        }
                    ]
                }
            ]
        };

        var value = new JsonObject
        {
            ["Amount1"] = 5,
            ["Amount2"] = 7
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_String_InCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Field",
            Condition = RuleCondition.In,
            Value = new List<string> { "Foo" }
        };

        var value = new JsonObject
        {
            ["Field"] = "Foo"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_String_InCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Field",
            Condition = RuleCondition.In,
            Value = new List<string> { "Foo" }
        };

        var value = new JsonObject
        {
            ["Field"] = "Bar"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_String_EqualToCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Field",
            Condition = RuleCondition.EqualTo,
            Value = "Foo"
        };

        var value = new JsonObject
        {
            ["Field"] = "Foo"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_String_EqualToCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Field",
            Condition = RuleCondition.EqualTo,
            Value = "Foo"
        };

        var value = new JsonObject
        {
            ["Field"] = "Bar"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_String_ContainsCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Field",
            Condition = RuleCondition.Contains,
            Value = "Foo"
        };

        var value = new JsonObject
        {
            ["Field"] = "Foobar"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_String_ContainsCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Field",
            Condition = RuleCondition.Contains,
            Value = "Foo"
        };

        var value = new JsonObject
        {
            ["Field"] = "Fobar"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_String_StartsWithCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Field",
            Condition = RuleCondition.StartsWith,
            Value = "Foo"
        };

        var value = new JsonObject
        {
            ["Field"] = "Foobar"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_String_StartsWithCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Field",
            Condition = RuleCondition.StartsWith,
            Value = "bar"
        };

        var value = new JsonObject
        {
            ["Field"] = "Foobar"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_String_EndsWithCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Field",
            Condition = RuleCondition.EndsWith,
            Value = "bar"
        };

        var value = new JsonObject
        {
            ["Field"] = "Foobar"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_String_EndsWithCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Field",
            Condition = RuleCondition.EndsWith,
            Value = "Foo"
        };

        var value = new JsonObject
        {
            ["Field"] = "Bar"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Theory]
    [InlineData(9, 50000000, false)]
    [InlineData(50000001, 50000000, true)]
    [InlineData(100, 99, true)]
    [InlineData(99, 100, false)]
    public void Evaluate_Long_GreaterThanCondition_OrdersNumerically(long amount, long threshold, bool expected)
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.GreaterThan,
            Value = threshold
        };

        var value = new JsonObject
        {
            ["Amount"] = amount
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(9, 50000000, true)]
    [InlineData(50000001, 50000000, false)]
    public void Evaluate_Long_LessThanCondition_OrdersNumerically(long amount, long threshold, bool expected)
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.LessThan,
            Value = threshold
        };

        var value = new JsonObject
        {
            ["Amount"] = amount
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(10.5, 10.05, true)]
    [InlineData(10.05, 10.5, false)]
    public void Evaluate_Double_GreaterThanCondition_OrdersNumerically(double amount, double threshold, bool expected)
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.GreaterThan,
            Value = threshold
        };

        var value = new JsonObject
        {
            ["Amount"] = amount
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Evaluate_MixedTypes_GreaterThanCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.GreaterThan,
            Value = 5
        };

        var value = new JsonObject
        {
            ["Amount"] = "Foo"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_MixedTypes_LessThanEqualCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.LessThanEqual,
            Value = "Foo"
        };

        var value = new JsonObject
        {
            ["Amount"] = 5
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_MixedTypes_EqualToCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.EqualTo,
            Value = "5"
        };

        var value = new JsonObject
        {
            ["Amount"] = 5
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_MissingField_GreaterThanCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.GreaterThan,
            Value = 5
        };

        var value = new JsonObject
        {
            ["OtherAmount"] = 6
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_Long_InCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.In,
            Value = new List<object> { 5L, 50000000L }
        };

        var value = new JsonObject
        {
            ["Amount"] = 50000000
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_Bool_EqualToCondition_ReturnsTrue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "IsHighRisk",
            Condition = RuleCondition.EqualTo,
            Value = true
        };

        var value = new JsonObject
        {
            ["IsHighRisk"] = true
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_NullField_ContainsCondition_ReturnsFalse()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Field",
            Condition = RuleCondition.Contains,
            Value = null
        };

        var value = new JsonObject
        {
            ["Field"] = "Foobar"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Trace_Leaf_RecordsExpectedAndActual()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.GreaterThan,
            Value = 10000
        };

        var value = new JsonObject
        {
            ["Amount"] = 50000
        };

        // Act
        var actual = RuleHelper.Trace(rule, value, RuleMatchPolarity.MatchesWhenTrue, Now);

        // Assert
        var conditionMatch = Assert.Single(actual);
        Assert.Equal("Amount", conditionMatch.Field);
        Assert.Equal(RuleCondition.GreaterThan, conditionMatch.Condition);
        Assert.Equal("10000", conditionMatch.Expected!.ToJsonString());
        Assert.Equal("50000", conditionMatch.Actual!.ToJsonString());
        Assert.True(conditionMatch.Matched);
    }

    [Fact]
    public void Trace_MissingField_RecordsNullActual()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.GreaterThan,
            Value = 10000
        };

        var value = new JsonObject
        {
            ["Currency"] = "GBP"
        };

        // Act
        var actual = RuleHelper.Trace(rule, value, RuleMatchPolarity.MatchesWhenTrue, Now);

        // Assert
        var conditionMatch = Assert.Single(actual);
        Assert.Null(conditionMatch.Actual);
        Assert.False(conditionMatch.Matched);
    }

    [Fact]
    public void Trace_And_RecordsEveryLeafWithItsOwnResult()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            And =
            [
                new RuleBodyModel { Field = "Amount", Condition = RuleCondition.GreaterThan, Value = 10000 },
                new RuleBodyModel { Field = "Currency", Condition = RuleCondition.EqualTo, Value = "USD" }
            ]
        };

        var value = new JsonObject
        {
            ["Amount"] = 50000,
            ["Currency"] = "GBP"
        };

        // Act
        var actual = RuleHelper.Trace(rule, value, RuleMatchPolarity.MatchesWhenTrue, Now);

        // Assert
        Assert.Equal(2, actual.Count);
        Assert.True(actual[0].Matched);
        Assert.Equal("Amount", actual[0].Field);
        Assert.False(actual[1].Matched);
        Assert.Equal("Currency", actual[1].Field);
        Assert.Equal("\"GBP\"", actual[1].Actual!.ToJsonString());
    }

    [Fact]
    public void Trace_Or_RecordsEveryBranch()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Or =
            [
                new RuleBodyModel { Field = "Amount", Condition = RuleCondition.GreaterThan, Value = 10000 },
                new RuleBodyModel { Field = "Country", Condition = RuleCondition.In, Value = new[] { "IR", "KP" } }
            ]
        };

        var value = new JsonObject
        {
            ["Amount"] = 500,
            ["Country"] = "KP"
        };

        // Act
        var actual = RuleHelper.Trace(rule, value, RuleMatchPolarity.MatchesWhenTrue, Now);

        // Assert
        Assert.Equal(2, actual.Count);
        Assert.False(actual[0].Matched);
        Assert.True(actual[1].Matched);
        Assert.Equal("[\"IR\",\"KP\"]", actual[1].Expected!.ToJsonString());
    }

    [Fact]
    public void Trace_Not_FoldsTheNegationIntoTheLeaf()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Not =
            [
                new RuleBodyModel { Field = "Currency", Condition = RuleCondition.EqualTo, Value = "GBP" }
            ]
        };

        var value = new JsonObject
        {
            ["Currency"] = "GBP"
        };

        // Act
        var actual = RuleHelper.Trace(rule, value, RuleMatchPolarity.MatchesWhenTrue, Now);

        // Assert
        var conditionMatch = Assert.Single(actual);
        Assert.Equal("Currency", conditionMatch.Field);
        Assert.False(conditionMatch.Matched);
    }

    [Fact]
    public void Trace_NestedNot_CancelsBackToTheLeafResult()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Not =
            [
                new RuleBodyModel
                {
                    Not =
                    [
                        new RuleBodyModel { Field = "Currency", Condition = RuleCondition.EqualTo, Value = "GBP" }
                    ]
                }
            ]
        };

        var value = new JsonObject
        {
            ["Currency"] = "GBP"
        };

        // Act
        var actual = RuleHelper.Trace(rule, value, RuleMatchPolarity.MatchesWhenTrue, Now);

        // Assert
        var conditionMatch = Assert.Single(actual);
        Assert.True(conditionMatch.Matched);
    }

    [Fact]
    public void Trace_MatchesWhenFalse_AgreesWithTheFiredOutcome()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Currency",
            Condition = RuleCondition.EqualTo,
            Value = "GBP"
        };

        var value = new JsonObject
        {
            ["Currency"] = "USD"
        };

        // Act
        var matched = RuleHelper.Matches(rule, value, RuleMatchPolarity.MatchesWhenFalse, Now);
        var actual = RuleHelper.Trace(rule, value, RuleMatchPolarity.MatchesWhenFalse, Now);

        // Assert
        Assert.True(matched);
        var conditionMatch = Assert.Single(actual);
        Assert.Equal("Currency", conditionMatch.Field);
        Assert.True(conditionMatch.Matched);
    }

    [Fact]
    public void Trace_AndAndOr_RecordsOnlyTheBranchEvaluateWalks()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            And = [new RuleBodyModel { Field = "Amount", Condition = RuleCondition.GreaterThan, Value = 10000 }],
            Or = [new RuleBodyModel { Field = "Currency", Condition = RuleCondition.EqualTo, Value = "GBP" }]
        };

        var value = new JsonObject
        {
            ["Amount"] = 50000,
            ["Currency"] = "GBP"
        };

        // Act
        var actual = RuleHelper.Trace(rule, value, RuleMatchPolarity.MatchesWhenTrue, Now);

        // Assert
        var conditionMatch = Assert.Single(actual);
        Assert.Equal("Amount", conditionMatch.Field);
    }

    [Fact]
    public void Evaluate_DottedPath_ReadsTheNestedField()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "card.bin",
            Condition = RuleCondition.EqualTo,
            Value = "453201"
        };

        var value = new JsonObject
        {
            ["card"] = new JsonObject { ["bin"] = "453201" }
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_IndexedPath_ReadsThatElement()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "parties[0].country",
            Condition = RuleCondition.EqualTo,
            Value = "IR"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, Parties(), Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Evaluate_AnyQuantifier_MatchesWhenOneElementQualifies()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "parties[*].country",
            Quantifier = RuleQuantifier.Any,
            Condition = RuleCondition.EqualTo,
            Value = "IR"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, Parties(), Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_AllQuantifier_DoesNotMatchWhenOneElementDoesNot()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "parties[*].country",
            Quantifier = RuleQuantifier.All,
            Condition = RuleCondition.EqualTo,
            Value = "IR"
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, Parties(), Now);

        // Assert
        Assert.False(actual);
    }

    [Theory]
    [InlineData(RuleQuantifier.Any)]
    [InlineData(RuleQuantifier.All)]
    public void Evaluate_QuantifierOverAnEmptyCollection_DoesNotMatch(RuleQuantifier quantifier)
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "parties[*].country",
            Quantifier = quantifier,
            Condition = RuleCondition.EqualTo,
            Value = "IR"
        };

        var value = new JsonObject { ["parties"] = new JsonArray() };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Theory]
    [InlineData("GBP", false)]
    [InlineData("USD", true)]
    public void Evaluate_NotEqualToCondition_NegatesTheComparison(string currency, bool expected)
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Currency",
            Condition = RuleCondition.NotEqualTo,
            Value = "GBP"
        };

        var value = new JsonObject { ["Currency"] = currency };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(RuleCondition.NotEqualTo)]
    [InlineData(RuleCondition.NotIn)]
    public void Evaluate_NegativeConditionOnAMissingField_DoesNotMatch(RuleCondition condition)
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Currency",
            Condition = condition,
            Value = condition == RuleCondition.NotIn ? new List<object?> { "GBP" } : "GBP"
        };

        var value = new JsonObject { ["Amount"] = 100 };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Theory]
    [InlineData("GBP", false)]
    [InlineData("IRR", true)]
    public void Evaluate_NotInCondition_MatchesWhatTheListDoesNotHold(string currency, bool expected)
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Currency",
            Condition = RuleCondition.NotIn,
            Value = new List<object?> { "GBP", "EUR" }
        };

        var value = new JsonObject { ["Currency"] = currency };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(499, false)]
    [InlineData(500, true)]
    [InlineData(9000, true)]
    [InlineData(9001, false)]
    public void Evaluate_BetweenCondition_IsInclusiveOfBothBounds(int amount, bool expected)
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Amount",
            Condition = RuleCondition.Between,
            Value = new List<object?> { 500, 9000 }
        };

        var value = new JsonObject { ["Amount"] = amount };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("GB-1234", true)]
    [InlineData("gb-1234", false)]
    [InlineData("GB-12", false)]
    public void Evaluate_MatchesCondition_AppliesTheRegularExpression(string reference, bool expected)
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Reference",
            Condition = RuleCondition.Matches,
            Value = "^GB-[0-9]{4}$"
        };

        var value = new JsonObject { ["Reference"] = reference };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Evaluate_MatchesConditionWithAPatternTheEngineCannotRun_DoesNotMatch()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Reference",
            Condition = RuleCondition.Matches,
            Value = "GB-[0-9"
        };

        var value = new JsonObject { ["Reference"] = "GB-1234" };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Theory]
    [InlineData("2024-03-01T00:00:00.500Z", true)]
    [InlineData("2024-03-01T00:00:00Z", false)]
    [InlineData("2024-02-29T23:59:59Z", false)]
    public void Evaluate_GreaterThanCondition_OrdersTimestampsAsInstants(string settledAt, bool expected)
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "SettledAt",
            Condition = RuleCondition.GreaterThan,
            Value = "2024-03-01T00:00:00Z"
        };

        var value = new JsonObject { ["SettledAt"] = settledAt };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(RuleCondition.EqualTo, true)]
    [InlineData(RuleCondition.GreaterThan, false)]
    public void Evaluate_TimestampsCarryingDifferentOffsets_CompareTheSameInstant(RuleCondition condition, bool expected)
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "SettledAt",
            Condition = condition,
            Value = "2024-06-01T09:00:00Z"
        };

        var value = new JsonObject { ["SettledAt"] = "2024-06-01T11:00:00+02:00" };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Evaluate_GreaterThanCondition_OrdersADateLikeStringAsText()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "Version",
            Condition = RuleCondition.GreaterThan,
            Value = "1.10"
        };

        var value = new JsonObject { ["Version"] = "1.5" };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_ExpressionLeaf_ComparesTheComputedValue()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Expression = new RuleExpressionModel
            {
                Function = RuleFunction.Sum,
                Arguments = [new RuleExpressionModel { Field = "parties[*].amount" }]
            },
            Condition = RuleCondition.GreaterThan,
            Value = 250
        };

        var value = new JsonObject
        {
            ["parties"] = new JsonArray(
                new JsonObject { ["amount"] = 100 },
                new JsonObject { ["amount"] = 200 })
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_TemporalExpressionLeaf_ReadsTheEvaluationClock()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Expression = new RuleExpressionModel
            {
                Function = RuleFunction.DaysSince,
                Arguments = [new RuleExpressionModel { Field = "account.openedAt" }]
            },
            Condition = RuleCondition.LessThan,
            Value = 30
        };

        var value = new JsonObject
        {
            ["account"] = new JsonObject { ["openedAt"] = "2026-08-01T12:00:00Z" }
        };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Evaluate_ExpressionThatComputesNothing_DoesNotMatch()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Expression = new RuleExpressionModel
            {
                Function = RuleFunction.DaysSince,
                Arguments = [new RuleExpressionModel { Field = "account.openedAt" }]
            },
            Condition = RuleCondition.LessThan,
            Value = 30
        };

        var value = new JsonObject { ["Amount"] = 100 };

        // Act
        var actual = RuleHelper.Evaluate(rule, value, Now);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Trace_QuantifiedLeaf_RecordsEveryValueTheQuantifierWeighed()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Field = "parties[*].country",
            Quantifier = RuleQuantifier.Any,
            Condition = RuleCondition.EqualTo,
            Value = "IR"
        };

        // Act
        var actual = RuleHelper.Trace(rule, Parties(), RuleMatchPolarity.MatchesWhenTrue, Now);

        // Assert
        var conditionMatch = Assert.Single(actual);
        Assert.Equal("parties[*].country", conditionMatch.Field);
        Assert.Equal(RuleQuantifier.Any, conditionMatch.Quantifier);
        Assert.True(conditionMatch.Matched);
        Assert.Equal("""["GB","IR"]""", conditionMatch.Actual!.ToJsonString());
    }

    [Fact]
    public void Trace_ExpressionLeaf_RecordsTheExpressionAndWhatItComputed()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Expression = new RuleExpressionModel
            {
                Function = RuleFunction.Count,
                Arguments = [new RuleExpressionModel { Field = "parties[*].country" }]
            },
            Condition = RuleCondition.GreaterThanEqual,
            Value = 2
        };

        // Act
        var actual = RuleHelper.Trace(rule, Parties(), RuleMatchPolarity.MatchesWhenTrue, Now);

        // Assert
        var conditionMatch = Assert.Single(actual);
        Assert.Equal("count(parties[*].country)", conditionMatch.Field);
        Assert.Equal("2", conditionMatch.Actual!.ToJsonString());
        Assert.True(conditionMatch.Matched);
    }

    private static JsonObject Parties()
    {
        return new JsonObject
        {
            ["parties"] = new JsonArray(
                new JsonObject { ["country"] = "GB" },
                new JsonObject { ["country"] = "IR" })
        };
    }
}
