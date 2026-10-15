using System.Text.Json;
using System.Text.Json.Nodes;
using Core.Enums;
using Core.Helpers;
using Core.Models.Rules;
using Core.Models.TransactionSchemas;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class TransactionSchemaHelperTests : BaseTestClass
{
    [Fact]
    public void ValidateRuleBody_GivenKnownFieldAndMatchingLiteral_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number));
        var ruleBody = Leaf("amount", RuleCondition.GreaterThan, Literal(10000));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenMisspelledField_ReportsUnknownField()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number));
        var ruleBody = Leaf("amont", RuleCondition.GreaterThan, Literal(10000));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                Field = "amont",
                Message = "is not a field on the transaction schema"
            });
    }

    [Fact]
    public void ValidateRuleBody_GivenFieldDifferingOnlyByCase_ReportsUnknownField()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number));
        var ruleBody = Leaf("Amount", RuleCondition.GreaterThan, Literal(10000));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle();
    }

    [Fact]
    public void ValidateRuleBody_GivenOrderingOnAStringField_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(Field("country", SchemaFieldType.String));
        var ruleBody = Leaf("country", RuleCondition.GreaterThanEqual, Literal("F"));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenOrderingOnAStringFieldAgainstANumber_ReportsTypeMismatch()
    {
        // Arrange
        var schema = CreateSchema(Field("country", SchemaFieldType.String));
        var ruleBody = Leaf("country", RuleCondition.GreaterThan, Literal(10));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("is a string but the rule compares it with a number");
    }

    [Fact]
    public void ValidateRuleBody_GivenSubstringConditionOnANumberField_ReportsUnusableCondition()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number));
        var ruleBody = Leaf("amount", RuleCondition.Contains, Literal("50"));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("is a number, so 'Contains' cannot be applied to it");
    }

    [Fact]
    public void ValidateRuleBody_GivenLiteralOfTheWrongType_ReportsTypeMismatch()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number));
        var ruleBody = Leaf("amount", RuleCondition.EqualTo, Literal("10000"));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("is a number but the rule compares it with a string");
    }

    [Fact]
    public void ValidateRuleBody_GivenIsNullOnARequiredField_ReportsUnreachableCondition()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number, isRequired: true));
        var ruleBody = new RuleBodyModel
        {
            Field = "amount",
            Condition = RuleCondition.IsNull
        };

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("is required by the transaction schema, so 'IsNull' can never match");
    }

    [Fact]
    public void ValidateRuleBody_GivenIsNullOnARequiredFieldInsideNot_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(Field("customerId", SchemaFieldType.String, isRequired: true));
        var ruleBody = new RuleBodyModel
        {
            Not =
            [
                new RuleBodyModel
                {
                    Field = "customerId",
                    Condition = RuleCondition.IsNull
                }
            ]
        };

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenIsNullOnARequiredFieldInsideNestedNots_ReportsUnreachableCondition()
    {
        // Arrange
        var schema = CreateSchema(Field("customerId", SchemaFieldType.String, isRequired: true));
        var ruleBody = new RuleBodyModel
        {
            Not =
            [
                new RuleBodyModel
                {
                    Not =
                    [
                        new RuleBodyModel
                        {
                            Field = "customerId",
                            Condition = RuleCondition.IsNull
                        }
                    ]
                }
            ]
        };

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("is required by the transaction schema, so 'IsNull' can never match");
    }

    [Fact]
    public void ValidateRuleBody_GivenIsNullOnAnOptionalField_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(Field("memo", SchemaFieldType.String));
        var ruleBody = new RuleBodyModel
        {
            Field = "memo",
            Condition = RuleCondition.IsNull
        };

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenValueOutsideTheEnumeration_ReportsDisallowedValue()
    {
        // Arrange
        var schema = CreateSchema(Field("currency", SchemaFieldType.String, allowedValues: ["GBP", "EUR"]));
        var ruleBody = Leaf("currency", RuleCondition.EqualTo, Literal("USD"));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("only allows 'GBP', 'EUR' but the rule compares it with 'USD'");
    }

    [Theory]
    [InlineData(RuleCondition.StartsWith, "G")]
    [InlineData(RuleCondition.Contains, "BP")]
    [InlineData(RuleCondition.EndsWith, "R")]
    public void ValidateRuleBody_GivenASubstringConditionOnAnEnumeratedField_ReturnsNoViolations(RuleCondition condition, string comparand)
    {
        // Arrange
        var schema = CreateSchema(Field("currency", SchemaFieldType.String, allowedValues: ["GBP", "EUR"]));
        var ruleBody = Leaf("currency", condition, Literal(comparand));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenASubstringConditionAgainstANumber_ReportsTypeMismatch()
    {
        // Arrange
        var schema = CreateSchema(Field("currency", SchemaFieldType.String, allowedValues: ["GBP", "EUR"]));
        var ruleBody = Leaf("currency", RuleCondition.StartsWith, Literal(10));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("is a string but the rule compares it with a number");
    }

    [Fact]
    public void ValidateRuleBody_GivenInWithAnOutOfEnumerationElement_ReportsDisallowedValue()
    {
        // Arrange
        var schema = CreateSchema(Field("currency", SchemaFieldType.String, allowedValues: ["GBP", "EUR"]));
        var ruleBody = Leaf("currency", RuleCondition.In, Literal(new[] { "GBP", "USD" }));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Contain("'USD'");
    }

    [Fact]
    public void ValidateRuleBody_GivenInWithoutAList_ReportsMissingList()
    {
        // Arrange
        var schema = CreateSchema(Field("currency", SchemaFieldType.String));
        var ruleBody = Leaf("currency", RuleCondition.In, Literal("GBP"));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("is compared with 'In', which needs a list of values");
    }

    [Fact]
    public void ValidateRuleBody_GivenAnUnknownFieldInsideAnUnreachedBranch_StillReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number));
        var ruleBody = new RuleBodyModel
        {
            And =
            [
                Leaf("amount", RuleCondition.GreaterThan, Literal(10000)),
                new RuleBodyModel
                {
                    Or = [Leaf("amont", RuleCondition.LessThan, Literal(5))]
                }
            ]
        };

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Field.Should().Be("amont");
    }

    [Fact]
    public void ValidateRuleBody_GivenAnUnknownFieldInsideNot_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number));
        var ruleBody = new RuleBodyModel
        {
            Not = [Leaf("beneficiary", RuleCondition.EqualTo, Literal("x"))]
        };

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Field.Should().Be("beneficiary");
    }

    [Fact]
    public void ValidatePayload_GivenAPayloadMatchingTheSchema_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(
            Field("amount", SchemaFieldType.Number, isRequired: true),
            Field("currency", SchemaFieldType.String, isRequired: true, allowedValues: ["GBP", "EUR"]),
            Field("isDomestic", SchemaFieldType.Boolean));

        var payload = new JsonObject
        {
            ["amount"] = 500,
            ["currency"] = "GBP",
            ["isDomestic"] = true
        };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePayload_GivenAMissingRequiredField_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number, isRequired: true));
        var payload = new JsonObject
        {
            ["amont"] = 500
        };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("is required by the transaction schema but the payload does not carry it");
    }

    [Fact]
    public void ValidatePayload_GivenAMissingOptionalField_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(Field("memo", SchemaFieldType.String));
        var payload = new JsonObject
        {
            ["amount"] = 500
        };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePayload_GivenAFieldOfTheWrongType_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number, isRequired: true));
        var payload = new JsonObject
        {
            ["amount"] = "500"
        };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("must be a number but the payload carries a string");
    }

    [Fact]
    public void ValidatePayload_GivenAStructuredValueForAScalarField_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number, isRequired: true));
        var payload = new JsonObject
        {
            ["amount"] = new JsonObject { ["value"] = 500 }
        };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("must be a number but the payload carries a structured value");
    }

    [Fact]
    public void ValidatePayload_GivenAValueOutsideTheEnumeration_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("currency", SchemaFieldType.String, isRequired: true, allowedValues: ["GBP", "EUR"]));
        var payload = new JsonObject
        {
            ["currency"] = "USD"
        };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("must be one of 'GBP', 'EUR' but the payload carries 'USD'");
    }

    [Fact]
    public void ValidatePayload_GivenFieldsNotOnTheSchema_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number, isRequired: true));
        var payload = new JsonObject
        {
            ["amount"] = 500,
            ["acquirerReference"] = "abc"
        };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePayload_GivenAnExplicitNullForARequiredField_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number, isRequired: true));
        var payload = new JsonObject
        {
            ["amount"] = null
        };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().ContainSingle();
    }

    private static RuleBodyModel Leaf(string field, RuleCondition condition, object value)
    {
        return new RuleBodyModel
        {
            Field = field,
            Condition = condition,
            Value = value
        };
    }

    // Rule literals arrive off the wire as JsonElement, so the tests use the same shape.
    private static object Literal(object value)
    {
        return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(value));
    }

    private static SchemaField Field(string name, SchemaFieldType type, bool isRequired = false, List<string>? allowedValues = null)
    {
        return new SchemaField
        {
            Name = new SchemaFieldName(name),
            Type = type,
            IsRequired = new IsRequired(isRequired),
            AllowedValues = allowedValues
        };
    }

    private static TransactionSchema CreateSchema(params SchemaField[] fields)
    {
        return new TransactionSchema
        {
            Mode = Mode.Live,
            TransactionSchemaId = new TransactionSchemaId(Guid.NewGuid()),
            OrganizationId = new OrganizationId(Guid.NewGuid()),
            Fields = fields.ToList(),
            CurrentVersionId = new TransactionSchemaVersionId(Guid.NewGuid()),
            CurrentVersionNumber = new VersionNumber(1),
            CreatedByUserId = new ApplicationUserId(Guid.NewGuid()),
            CreatedAt = new CreatedAt(DateTimeOffset.UnixEpoch),
            UpdatedAt = new UpdatedAt(DateTimeOffset.UnixEpoch),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }

    [Fact]
    public void ValidateRuleBody_GivenADottedPathTheSchemaDeclares_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(Object("card", Field("bin", SchemaFieldType.String)));
        var ruleBody = Leaf("card.bin", RuleCondition.StartsWith, Literal("4532"));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenAMemberTheNestedObjectDoesNotDeclare_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Object("card", Field("bin", SchemaFieldType.String)));
        var ruleBody = Leaf("card.scheme", RuleCondition.EqualTo, Literal("VISA"));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("is not a field on the transaction schema — 'scheme' is not declared");
    }

    [Fact]
    public void ValidateRuleBody_GivenAPathThroughAScalar_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("card", SchemaFieldType.String));
        var ruleBody = Leaf("card.bin", RuleCondition.EqualTo, Literal("4532"));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("reads a member of 'card', which the schema declares as a string");
    }

    [Fact]
    public void ValidateRuleBody_GivenAnIndexOnSomethingThatIsNotAnArray_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Object("card", Field("bin", SchemaFieldType.String)));
        var ruleBody = Leaf("card[0].bin", RuleCondition.EqualTo, Literal("4532"));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("indexes 'card', which the schema declares as a nested object rather than an array");
    }

    [Fact]
    public void ValidateRuleBody_GivenAWildcardOverADeclaredArray_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(ArrayOfObjects("parties", Field("country", SchemaFieldType.String)));
        var ruleBody = Quantified("parties[*].country", RuleQuantifier.Any, RuleCondition.EqualTo, Literal("IR"));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenAnElementLiteralOfTheWrongType_ReportsTypeMismatch()
    {
        // Arrange
        var schema = CreateSchema(ArrayOfObjects("parties", Field("country", SchemaFieldType.String)));
        var ruleBody = Quantified("parties[*].country", RuleQuantifier.All, RuleCondition.EqualTo, Literal(10));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("is a string but the rule compares it with a number");
    }

    [Fact]
    public void ValidateRuleBody_GivenAComparisonAgainstTheArrayItself_ReportsThatItIsStructured()
    {
        // Arrange
        var schema = CreateSchema(ArrayOfObjects("parties", Field("country", SchemaFieldType.String)));
        var ruleBody = Leaf("parties", RuleCondition.EqualTo, Literal("IR"));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("is an array, so 'EqualTo' cannot be applied to it — address a field inside it instead");
    }

    [Fact]
    public void ValidateRuleBody_GivenIsNullOnARequiredNestedField_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(Object("card", Field("bin", SchemaFieldType.String, isRequired: true)));
        var ruleBody = new RuleBodyModel { Field = "card.bin", Condition = RuleCondition.IsNull };

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenATimestampComparedWithATimestamp_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(Field("occurredAt", SchemaFieldType.DateTime));
        var ruleBody = Leaf("occurredAt", RuleCondition.GreaterThan, Literal("2026-01-01T00:00:00Z"));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenATimestampComparedWithSomethingElse_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("occurredAt", SchemaFieldType.DateTime));
        var ruleBody = Leaf("occurredAt", RuleCondition.GreaterThan, Literal("yesterday"));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("is a timestamp but the rule compares it with a value that is not one");
    }

    [Fact]
    public void ValidateRuleBody_GivenATemporalExpressionOverATimestamp_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(Object("account", Field("openedAt", SchemaFieldType.DateTime)));
        var ruleBody = Computed(
            Function(RuleFunction.DaysSince, PathExpression("account.openedAt")),
            RuleCondition.LessThan,
            Literal(30));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenATemporalExpressionOverANumber_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number));
        var ruleBody = Computed(
            Function(RuleFunction.DaysSince, PathExpression("amount")),
            RuleCondition.LessThan,
            Literal(30));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                Field = "daysSince(amount)",
                Message = "'DaysSince' reads a timestamp but was given a number"
            });
    }

    [Fact]
    public void ValidateRuleBody_GivenAnAggregateOverAWildcardPathOfNumbers_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(ArrayOfObjects("parties", Field("amount", SchemaFieldType.Number)));
        var ruleBody = Computed(
            Function(RuleFunction.Sum, PathExpression("parties[*].amount")),
            RuleCondition.GreaterThan,
            Literal(10000));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenCountOverAWildcardPathOfObjects_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(ArrayOfObjects("parties", Field("country", SchemaFieldType.String)));
        var ruleBody = Computed(
            Function(RuleFunction.Count, PathExpression("parties[*]")),
            RuleCondition.GreaterThan,
            Literal(2));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenAnAggregateOtherThanCountOverObjects_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(ArrayOfObjects("parties", Field("country", SchemaFieldType.String)));
        var ruleBody = Computed(
            Function(RuleFunction.Sum, PathExpression("parties[*]")),
            RuleCondition.GreaterThan,
            Literal(2));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("is a nested object, so it cannot be computed with — address a field inside it instead");
    }

    [Fact]
    public void ValidateRuleBody_GivenAnAggregateOverStrings_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(ArrayOfObjects("parties", Field("country", SchemaFieldType.String)));
        var ruleBody = Computed(
            Function(RuleFunction.Sum, PathExpression("parties[*].country")),
            RuleCondition.GreaterThan,
            Literal(10000));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("'Sum' reduces numbers but was given a string");
    }

    [Fact]
    public void ValidateRuleBody_GivenAnAggregateOverAPathResolvingToOneValue_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number));
        var ruleBody = Computed(
            Function(RuleFunction.Sum, PathExpression("amount")),
            RuleCondition.GreaterThan,
            Literal(10000));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("resolves to a single value, so there is nothing for the aggregate to reduce — address it with '[*]'");
    }

    [Fact]
    public void ValidateRuleBody_GivenAComputedValueComparedWithTheWrongType_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number), Field("fee", SchemaFieldType.Number));
        var ruleBody = Computed(
            Function(RuleFunction.Add, PathExpression("amount"), PathExpression("fee")),
            RuleCondition.EqualTo,
            Literal("free"));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("computes a number but the rule compares it with a string");
    }

    [Fact]
    public void ValidateRuleBody_GivenAnExpressionOverAnUndeclaredField_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number));
        var ruleBody = Computed(
            Function(RuleFunction.Abs, PathExpression("amont")),
            RuleCondition.GreaterThan,
            Literal(10));

        // Act
        var violations = TransactionSchemaHelper.ValidateRuleBody(ruleBody, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Field.Should().Be("amont");
    }

    [Fact]
    public void ValidatePayload_GivenANestedObjectMatchingTheSchema_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(Object("card", Field("bin", SchemaFieldType.String, isRequired: true)));
        var payload = new JsonObject
        {
            ["card"] = new JsonObject { ["bin"] = "453201" }
        };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePayload_GivenANestedRequiredFieldTheObjectDoesNotCarry_ReportsItByItsPath()
    {
        // Arrange
        var schema = CreateSchema(Object("card", Field("bin", SchemaFieldType.String, isRequired: true)));
        var payload = new JsonObject
        {
            ["card"] = new JsonObject { ["scheme"] = "VISA" }
        };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Field.Should().Be("card.bin");
    }

    [Fact]
    public void ValidatePayload_GivenAnArrayElementOfTheWrongType_ReportsItByItsIndex()
    {
        // Arrange
        var schema = CreateSchema(ArrayOfObjects("parties", Field("country", SchemaFieldType.String)));
        var payload = new JsonObject
        {
            ["parties"] = new JsonArray(
                new JsonObject { ["country"] = "GB" },
                new JsonObject { ["country"] = 44 })
        };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Field.Should().Be("parties[1].country");
    }

    [Fact]
    public void ValidatePayload_GivenASingleValueWhereTheSchemaDeclaresAnArray_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(ArrayOfObjects("parties", Field("country", SchemaFieldType.String)));
        var payload = new JsonObject { ["parties"] = "GB" };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("must be an array but the payload does not carry one");
    }

    [Fact]
    public void ValidatePayload_GivenAnArrayOfScalars_ChecksEachElement()
    {
        // Arrange
        var schema = CreateSchema(ArrayOf("tags", SchemaFieldType.String));
        var payload = new JsonObject { ["tags"] = new JsonArray("high-risk", 7) };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Field.Should().Be("tags[1]");
    }

    [Theory]
    [InlineData("2026-08-16T13:30:00Z")]
    [InlineData("2026-08-16T13:30:00+02:00")]
    public void ValidatePayload_GivenAnIsoTimestamp_ReturnsNoViolations(string occurredAt)
    {
        // Arrange
        var schema = CreateSchema(Field("occurredAt", SchemaFieldType.DateTime));
        var payload = new JsonObject { ["occurredAt"] = occurredAt };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePayload_GivenSomethingThatIsNotATimestamp_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("occurredAt", SchemaFieldType.DateTime));
        var payload = new JsonObject { ["occurredAt"] = "yesterday" };

        // Act
        var violations = TransactionSchemaHelper.ValidatePayload(payload, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Be("must be an ISO-8601 timestamp but the payload does not carry one");
    }

    private static RuleBodyModel Quantified(string field, RuleQuantifier quantifier, RuleCondition condition, object value)
    {
        return new RuleBodyModel
        {
            Field = field,
            Quantifier = quantifier,
            Condition = condition,
            Value = value
        };
    }

    private static RuleBodyModel Computed(RuleExpressionModel expression, RuleCondition condition, object value)
    {
        return new RuleBodyModel
        {
            Expression = expression,
            Condition = condition,
            Value = value
        };
    }

    private static RuleExpressionModel PathExpression(string field)
    {
        return new RuleExpressionModel { Field = field };
    }

    private static RuleExpressionModel Function(RuleFunction function, params RuleExpressionModel[] arguments)
    {
        return new RuleExpressionModel { Function = function, Arguments = arguments };
    }

    private static SchemaField Object(string name, params SchemaField[] fields)
    {
        return new SchemaField
        {
            Name = new SchemaFieldName(name),
            Type = SchemaFieldType.Object,
            IsRequired = new IsRequired(false),
            AllowedValues = null,
            Fields = fields.ToList()
        };
    }

    private static SchemaField ArrayOfObjects(string name, params SchemaField[] fields)
    {
        return new SchemaField
        {
            Name = new SchemaFieldName(name),
            Type = SchemaFieldType.Array,
            IsRequired = new IsRequired(false),
            AllowedValues = null,
            ElementType = SchemaFieldType.Object,
            Fields = fields.ToList()
        };
    }

    private static SchemaField ArrayOf(string name, SchemaFieldType elementType)
    {
        return new SchemaField
        {
            Name = new SchemaFieldName(name),
            Type = SchemaFieldType.Array,
            IsRequired = new IsRequired(false),
            AllowedValues = null,
            ElementType = elementType
        };
    }

    [Fact]
    public void ValidateVelocityProfile_GivenAMetricOverANumber_ReturnsNoViolations()
    {
        // Arrange
        var schema = CreateSchema(Field("amount", SchemaFieldType.Number));
        var metrics = new List<VelocityMetric> { Metric("amount", "amount") };

        // Act
        var violations = TransactionSchemaHelper.ValidateVelocityProfile(metrics, schema);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateVelocityProfile_GivenAMetricOverABoolean_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Field("isHighRisk", SchemaFieldType.Boolean));
        var metrics = new List<VelocityMetric> { Metric("highRisk", "isHighRisk") };

        // Act
        var violations = TransactionSchemaHelper.ValidateVelocityProfile(metrics, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Contain("neither a figure to total nor an identity to count");
    }

    [Fact]
    public void ValidateVelocityProfile_GivenAMetricOverANestedObject_ReportsIt()
    {
        // Arrange
        var schema = CreateSchema(Object("card", Field("bin", SchemaFieldType.String)));
        var metrics = new List<VelocityMetric> { Metric("card", "card") };

        // Act
        var violations = TransactionSchemaHelper.ValidateVelocityProfile(metrics, schema);

        // Assert
        violations.Should().ContainSingle()
            .Which.Message.Should().Contain("declare a field inside it instead");
    }

    private static VelocityMetric Metric(string name, string path)
    {
        return new VelocityMetric
        {
            Name = new VelocityMetricName(name),
            Path = new VelocityMetricPath(path)
        };
    }
}
