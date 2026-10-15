using System.Text.Json;
using System.Text.Json.Nodes;
using Core.Enums;
using Core.Models.PartyMappings;
using Core.Models.Rules;
using Core.Models.TransactionSchemas;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;

namespace Core.Helpers;

public static class TransactionSchemaHelper
{
    private static readonly RuleCondition[] SubstringConditions =
    [
        RuleCondition.Contains,
        RuleCondition.StartsWith,
        RuleCondition.EndsWith,
        RuleCondition.Matches
    ];

    private static readonly RuleCondition[] ListConditions =
    [
        RuleCondition.In,
        RuleCondition.NotIn
    ];

    private static readonly SchemaFieldType[] StructuredTypes =
    [
        SchemaFieldType.Object,
        SchemaFieldType.Array
    ];

    public static List<SchemaViolation> ValidateRuleBody(RuleBodyModel ruleBody, TransactionSchema schema)
    {
        var violations = new List<SchemaViolation>();

        CollectRuleViolations(ruleBody, schema.Fields, violations, false);

        return violations;
    }

    public static List<SchemaViolation> ValidatePayload(JsonObject payload, TransactionSchema schema)
    {
        var violations = new List<SchemaViolation>();

        CollectPayloadViolations(payload, schema.Fields, string.Empty, violations);

        return violations;
    }

    public static List<SchemaViolation> ValidatePartyMapping(List<PartyRoleMapping> mappings, TransactionSchema schema)
    {
        var violations = new List<SchemaViolation>();

        foreach (var mapping in mappings)
        {
            CollectPartyPathViolations(Combine(mapping.Path, mapping.IdentifierPath), "identify", schema.Fields, violations);

            if (mapping.DisplayNamePath.HasValue)
            {
                CollectPartyPathViolations(Combine(mapping.Path, mapping.DisplayNamePath.Value), "name", schema.Fields, violations);
            }

            foreach (var attributePath in mapping.AttributePaths ?? [])
            {
                CollectPartyPathViolations(Combine(mapping.Path, attributePath.Path), "describe", schema.Fields, violations);
            }
        }

        return violations;
    }

    public static List<SchemaViolation> ValidateVelocityProfile(List<VelocityMetric> metrics, TransactionSchema schema)
    {
        var violations = new List<SchemaViolation>();

        foreach (var metric in metrics)
        {
            if (!TryResolve(metric.Path, schema.Fields, out var resolution, out var failure))
            {
                violations.Add(Violation(metric.Path, failure));

                continue;
            }

            if (StructuredTypes.Contains(resolution!.Type))
            {
                violations.Add(Violation(metric.Path, $"is {Describe(resolution.Type)}, so it carries no value a metric can keep — declare a field inside it instead"));

                continue;
            }

            if (resolution.Type == SchemaFieldType.Boolean)
            {
                violations.Add(Violation(metric.Path, $"is {Describe(resolution.Type)}, so it is neither a figure to total nor an identity to count — declare the field the flag was decided from instead"));
            }
        }

        return violations;
    }

    public static bool TryResolveType(string path, TransactionSchema schema, out SchemaFieldType type)
    {
        type = SchemaFieldType.Object;

        if (!TryResolve(path, schema.Fields, out var resolution, out _))
        {
            return false;
        }

        type = resolution!.Type;

        return true;
    }

    public static string DescribeType(SchemaFieldType type)
    {
        return Describe(type);
    }

    private static void CollectRuleViolations(RuleBodyModel rule, List<SchemaField> roots, List<SchemaViolation> violations, bool negated)
    {
        if (rule.Expression != null)
        {
            CollectExpressionLeafViolations(rule, roots, violations);
        }
        else if (rule.Field != null)
        {
            if (!TryResolve(rule.Field, roots, out var resolution, out var failure))
            {
                violations.Add(Violation(rule.Field, failure));
            }
            else
            {
                CollectConditionViolations(rule, rule.Field, resolution!, violations, negated);
            }
        }

        foreach (var child in rule.Not ?? [])
        {
            CollectRuleViolations(child, roots, violations, !negated);
        }

        foreach (var child in (rule.And ?? []).Concat(rule.Or ?? []))
        {
            CollectRuleViolations(child, roots, violations, negated);
        }
    }

    private static void CollectConditionViolations(RuleBodyModel rule, string path, Resolution resolution, List<SchemaViolation> violations, bool negated)
    {
        if (rule.Condition == RuleCondition.IsNull)
        {
            if (resolution.Field.IsRequired && !negated && !resolution.Nested)
            {
                violations.Add(Violation(path, "is required by the transaction schema, so 'IsNull' can never match"));
            }

            return;
        }

        if (StructuredTypes.Contains(resolution.Type))
        {
            violations.Add(Violation(path, $"is {Describe(resolution.Type)}, so '{rule.Condition}' cannot be applied to it — address a field inside it instead"));

            return;
        }

        if (rule.Condition is { } substring && SubstringConditions.Contains(substring))
        {
            if (resolution.Type != SchemaFieldType.String)
            {
                violations.Add(Violation(path, $"is {Describe(resolution.Type)}, so '{substring}' cannot be applied to it"));

                return;
            }

            CollectLiteralViolations(rule.Value, path, resolution, violations, false);

            return;
        }

        if (rule.Condition is { } list && ListConditions.Contains(list))
        {
            if (!TryGetElements(rule.Value, out var elements))
            {
                violations.Add(Violation(path, $"is compared with '{list}', which needs a list of values"));

                return;
            }

            foreach (var element in elements)
            {
                CollectLiteralViolations(element, path, resolution, violations, true);
            }

            return;
        }

        if (rule.Condition == RuleCondition.Between)
        {
            if (!TryGetElements(rule.Value, out var bounds) || bounds.Count != 2)
            {
                violations.Add(Violation(path, "is compared with 'Between', which needs a lower and an upper bound"));

                return;
            }

            foreach (var bound in bounds)
            {
                CollectLiteralViolations(bound, path, resolution, violations, false);
            }

            return;
        }

        CollectLiteralViolations(rule.Value, path, resolution, violations, true);
    }

    private static void CollectExpressionLeafViolations(RuleBodyModel rule, List<SchemaField> roots, List<SchemaViolation> violations)
    {
        var description = RuleExpressionEvaluator.Describe(rule.Expression!);

        if (!TryInfer(rule.Expression!, roots, false, false, violations, out var type))
        {
            return;
        }

        if (rule.Condition == RuleCondition.IsNull)
        {
            return;
        }

        if (rule.Condition is { } substring && SubstringConditions.Contains(substring) && type != SchemaFieldType.String)
        {
            violations.Add(Violation(description, $"computes {Describe(type)}, so '{substring}' cannot be applied to it"));

            return;
        }

        if (rule.Condition is { } list && ListConditions.Contains(list))
        {
            if (!TryGetElements(rule.Value, out var elements))
            {
                violations.Add(Violation(description, $"is compared with '{list}', which needs a list of values"));

                return;
            }

            foreach (var element in elements)
            {
                CollectComputedLiteralViolations(element, description, type, violations);
            }

            return;
        }

        if (rule.Condition == RuleCondition.Between)
        {
            if (!TryGetElements(rule.Value, out var bounds) || bounds.Count != 2)
            {
                violations.Add(Violation(description, "is compared with 'Between', which needs a lower and an upper bound"));

                return;
            }

            foreach (var bound in bounds)
            {
                CollectComputedLiteralViolations(bound, description, type, violations);
            }

            return;
        }

        CollectComputedLiteralViolations(rule.Value, description, type, violations);
    }

    private static void CollectComputedLiteralViolations(object? literal, string description, SchemaFieldType type, List<SchemaViolation> violations)
    {
        if (type == SchemaFieldType.DateTime)
        {
            if (!IsTimestamp(literal))
            {
                violations.Add(Violation(description, "computes a timestamp but the rule compares it with a value that is not one"));
            }

            return;
        }

        if (!TryClassifyLiteral(literal, out var actual))
        {
            violations.Add(Violation(description, $"computes {Describe(type)} but the rule compares it with a structured value"));

            return;
        }

        if (actual != type)
        {
            violations.Add(Violation(description, $"computes {Describe(type)} but the rule compares it with {Describe(actual)}"));
        }
    }

    private static bool TryInfer(
        RuleExpressionModel expression,
        List<SchemaField> roots,
        bool expectMany,
        bool allowStructured,
        List<SchemaViolation> violations,
        out SchemaFieldType type)
    {
        type = default;

        if (expression.Field != null)
        {
            if (!TryResolve(expression.Field, roots, out var resolution, out var failure))
            {
                violations.Add(Violation(expression.Field, failure));

                return false;
            }

            if (resolution!.Multiple != expectMany)
            {
                violations.Add(Violation(
                    expression.Field,
                    expectMany
                        ? "resolves to a single value, so there is nothing for the aggregate to reduce — address it with '[*]'"
                        : "resolves to many values, so only an aggregate can read it"));

                return false;
            }

            if (StructuredTypes.Contains(resolution.Type) && !allowStructured)
            {
                violations.Add(Violation(expression.Field, $"is {Describe(resolution.Type)}, so it cannot be computed with — address a field inside it instead"));

                return false;
            }

            type = resolution.Type;

            return true;
        }

        if (expression.Function == null)
        {
            if (!TryClassifyLiteral(expression.Value, out type))
            {
                violations.Add(Violation(RuleExpressionEvaluator.Describe(expression), "is not a value the engine can compute with"));

                return false;
            }

            return true;
        }

        return TryInferFunction(expression, expression.Function.Value, roots, expectMany, violations, out type);
    }

    private static bool TryInferFunction(
        RuleExpressionModel expression,
        RuleFunction function,
        List<SchemaField> roots,
        bool expectMany,
        List<SchemaViolation> violations,
        out SchemaFieldType type)
    {
        type = SchemaFieldType.Number;

        var description = RuleExpressionEvaluator.Describe(expression);
        var arguments = expression.Arguments ?? [];

        if (expectMany)
        {
            violations.Add(Violation(description, "does not resolve to a list of values, so it cannot be aggregated"));

            return false;
        }

        if (arguments.Length != RuleExpressionEvaluator.Arity(function))
        {
            violations.Add(Violation(description, $"'{function}' takes {RuleExpressionEvaluator.Arity(function)} argument(s) but was given {arguments.Length}"));

            return false;
        }

        if (RuleExpressionEvaluator.IsAggregate(function))
        {
            if (!TryInfer(arguments[0], roots, true, function == RuleFunction.Count, violations, out var elementType))
            {
                return false;
            }

            if (function != RuleFunction.Count && elementType != SchemaFieldType.Number)
            {
                violations.Add(Violation(description, $"'{function}' reduces numbers but was given {Describe(elementType)}"));

                return false;
            }

            return true;
        }

        var expected = ExpectedArgumentType(function);

        foreach (var argument in arguments)
        {
            if (!TryInfer(argument, roots, false, false, violations, out var argumentType))
            {
                return false;
            }

            if (argumentType != expected)
            {
                violations.Add(Violation(description, $"'{function}' reads {Describe(expected)} but was given {Describe(argumentType)}"));

                return false;
            }
        }

        return true;
    }

    private static SchemaFieldType ExpectedArgumentType(RuleFunction function)
    {
        if (RuleExpressionEvaluator.IsTemporal(function))
        {
            return SchemaFieldType.DateTime;
        }

        return function == RuleFunction.Length ? SchemaFieldType.String : SchemaFieldType.Number;
    }

    private static void CollectLiteralViolations(object? literal, string path, Resolution resolution, List<SchemaViolation> violations, bool enforceAllowedValues)
    {
        if (resolution.Type == SchemaFieldType.DateTime)
        {
            if (!IsTimestamp(literal))
            {
                violations.Add(Violation(path, "is a timestamp but the rule compares it with a value that is not one"));
            }

            return;
        }

        if (!TryClassifyLiteral(literal, out var actual))
        {
            violations.Add(Violation(path, $"is {Describe(resolution.Type)} but the rule compares it with a structured value"));

            return;
        }

        if (actual != resolution.Type)
        {
            violations.Add(Violation(path, $"is {Describe(resolution.Type)} but the rule compares it with {Describe(actual)}"));

            return;
        }

        if (enforceAllowedValues &&
            resolution.Field.AllowedValues is { Count: > 0 } &&
            TryGetString(literal, out var value) &&
            !resolution.Field.AllowedValues.Contains(value, StringComparer.Ordinal))
        {
            violations.Add(Violation(path, $"only allows {Join(resolution.Field.AllowedValues)} but the rule compares it with '{value}'"));
        }
    }

    // Ordinal, because evaluation uses JsonObject's ordinal lookup; case-insensitive here would accept rules that never match.
    private static bool TryResolve(string path, List<SchemaField> roots, out Resolution? resolution, out string failure)
    {
        resolution = null;
        failure = "is not a field on the transaction schema";

        var declared = roots.FirstOrDefault(x => string.Equals(x.Name, path, StringComparison.Ordinal));

        if (declared != null)
        {
            resolution = new Resolution { Field = declared, Type = declared.Type, Multiple = false, Nested = false };

            return true;
        }

        if (!PayloadPath.TryParse(path, out var segments))
        {
            failure = "is not a path the engine can read";

            return false;
        }

        var declarations = roots;
        SchemaField? current = null;
        var type = SchemaFieldType.Object;
        var multiple = false;

        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];

            current = declarations?.FirstOrDefault(x => string.Equals(x.Name, segment.Name, StringComparison.Ordinal));

            if (current == null)
            {
                failure = segments.Count == 1
                    ? "is not a field on the transaction schema"
                    : $"is not a field on the transaction schema — '{segment.Name}' is not declared";

                return false;
            }

            type = current.Type;
            declarations = current.Fields;

            foreach (var accessor in segment.Accessors)
            {
                if (type != SchemaFieldType.Array)
                {
                    failure = $"indexes '{segment.Name}', which the schema declares as {Describe(type)} rather than an array";

                    return false;
                }

                type = current.ElementType ?? SchemaFieldType.String;
                multiple |= accessor.IsWildcard;
            }

            if (index < segments.Count - 1 && type != SchemaFieldType.Object)
            {
                failure = $"reads a member of '{segment.Name}', which the schema declares as {Describe(type)}";

                return false;
            }
        }

        resolution = new Resolution
        {
            Field = current!,
            Type = type,
            Multiple = multiple,
            Nested = segments.Count > 1 || segments[0].Accessors.Count > 0
        };

        return true;
    }

    private static string Combine(PartyPath? path, PartyPath relative)
    {
        return path.HasValue ? $"{path.Value}.{relative}" : relative;
    }

    private static void CollectPartyPathViolations(string path, string purpose, List<SchemaField> roots, List<SchemaViolation> violations)
    {
        if (!TryResolve(path, roots, out var resolution, out var failure))
        {
            violations.Add(Violation(path, failure));

            return;
        }

        if (resolution!.Type is not (SchemaFieldType.String or SchemaFieldType.Number))
        {
            violations.Add(Violation(path, $"is {Describe(resolution.Type)}, so it cannot {purpose} a party"));
        }
    }

    private static void CollectPayloadViolations(JsonObject payload, List<SchemaField> fields, string prefix, List<SchemaViolation> violations)
    {
        foreach (var field in fields)
        {
            var path = prefix + field.Name;

            if (!payload.TryGetPropertyValue(field.Name, out var node) || node is null)
            {
                if (field.IsRequired)
                {
                    violations.Add(Violation(path, "is required by the transaction schema but the payload does not carry it"));
                }

                continue;
            }

            CollectNodeViolations(node, field, field.Type, path, violations);
        }
    }

    private static void CollectNodeViolations(JsonNode node, SchemaField field, SchemaFieldType type, string path, List<SchemaViolation> violations)
    {
        switch (type)
        {
            case SchemaFieldType.Object:
                if (node is not JsonObject nested)
                {
                    violations.Add(Violation(path, "must be a nested object but the payload carries a single value"));

                    return;
                }

                CollectPayloadViolations(nested, field.Fields ?? [], path + ".", violations);

                return;

            case SchemaFieldType.Array:
                if (node is not JsonArray array)
                {
                    violations.Add(Violation(path, "must be an array but the payload does not carry one"));

                    return;
                }

                for (var index = 0; index < array.Count; index++)
                {
                    if (array[index] is { } element)
                    {
                        CollectNodeViolations(element, field, field.ElementType ?? SchemaFieldType.String, $"{path}[{index}]", violations);
                    }
                }

                return;

            case SchemaFieldType.DateTime:
                if (!TryGetString(node, out var timestamp) || !RuleValueHelper.TryGetDateTime(timestamp, out _))
                {
                    violations.Add(Violation(path, "must be an ISO-8601 timestamp but the payload does not carry one"));
                }

                return;

            default:
                CollectScalarViolations(node, field, type, path, violations);

                return;
        }
    }

    private static void CollectScalarViolations(JsonNode node, SchemaField field, SchemaFieldType type, string path, List<SchemaViolation> violations)
    {
        if (!TryClassifyNode(node, out var actual))
        {
            violations.Add(Violation(path, $"must be {Describe(type)} but the payload carries a structured value"));

            return;
        }

        if (actual != type)
        {
            violations.Add(Violation(path, $"must be {Describe(type)} but the payload carries {Describe(actual)}"));

            return;
        }

        if (field.AllowedValues is { Count: > 0 } &&
            TryGetString(node, out var value) &&
            !field.AllowedValues.Contains(value, StringComparer.Ordinal))
        {
            violations.Add(Violation(path, $"must be one of {Join(field.AllowedValues)} but the payload carries '{value}'"));
        }
    }

    private static bool IsTimestamp(object? literal)
    {
        return TryGetString(literal, out var value) && RuleValueHelper.TryGetDateTime(value, out _);
    }

    private static bool TryClassifyNode(JsonNode node, out SchemaFieldType type)
    {
        type = default;

        if (node is not JsonValue jsonValue)
        {
            return false;
        }

        return TryClassifyKind(jsonValue.GetValueKind(), out type);
    }

    private static bool TryClassifyLiteral(object? literal, out SchemaFieldType type)
    {
        type = default;

        switch (literal)
        {
            case null:
                return false;
            case JsonElement jsonElement:
                return TryClassifyKind(jsonElement.ValueKind, out type);
            case JsonNode jsonNode:
                return TryClassifyNode(jsonNode, out type);
            case string:
                type = SchemaFieldType.String;
                return true;
            case bool:
                type = SchemaFieldType.Boolean;
                return true;
            case decimal or double or float or long or int or short or byte:
                type = SchemaFieldType.Number;
                return true;
            default:
                return false;
        }
    }

    private static bool TryClassifyKind(JsonValueKind kind, out SchemaFieldType type)
    {
        type = default;

        switch (kind)
        {
            case JsonValueKind.String:
                type = SchemaFieldType.String;
                return true;
            case JsonValueKind.Number:
                type = SchemaFieldType.Number;
                return true;
            case JsonValueKind.True:
            case JsonValueKind.False:
                type = SchemaFieldType.Boolean;
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetElements(object? literal, out List<object?> elements)
    {
        switch (literal)
        {
            case JsonElement { ValueKind: JsonValueKind.Array } jsonElement:
                elements = jsonElement.EnumerateArray().Select(x => (object?)x).ToList();
                return true;
            case JsonArray jsonArray:
                elements = jsonArray.Select(x => (object?)x).ToList();
                return true;
            case string:
                elements = [];
                return false;
            case IEnumerable<object?> enumerable:
                elements = enumerable.ToList();
                return true;
            default:
                elements = [];
                return false;
        }
    }

    private static bool TryGetString(object? literal, out string value)
    {
        switch (literal)
        {
            case JsonElement { ValueKind: JsonValueKind.String } jsonElement:
                value = jsonElement.GetString() ?? string.Empty;
                return true;
            case JsonValue jsonValue when jsonValue.GetValueKind() == JsonValueKind.String:
                value = jsonValue.GetValue<string>();
                return true;
            case string stringValue:
                value = stringValue;
                return true;
            default:
                value = string.Empty;
                return false;
        }
    }

    private static SchemaViolation Violation(string field, string message)
    {
        return new SchemaViolation
        {
            Field = field,
            Message = message
        };
    }

    private static string Describe(SchemaFieldType type)
    {
        return type switch
        {
            SchemaFieldType.String => "a string",
            SchemaFieldType.Number => "a number",
            SchemaFieldType.Boolean => "a boolean",
            SchemaFieldType.DateTime => "a timestamp",
            SchemaFieldType.Object => "a nested object",
            SchemaFieldType.Array => "an array",
            _ => type.ToString().ToLowerInvariant()
        };
    }

    private static string Join(IEnumerable<string> values)
    {
        return string.Join(", ", values.Select(x => $"'{x}'"));
    }

    private sealed record Resolution
    {
        public required SchemaField Field { get; init; }
        public required SchemaFieldType Type { get; init; }
        public required bool Multiple { get; init; }
        public required bool Nested { get; init; }
    }
}
