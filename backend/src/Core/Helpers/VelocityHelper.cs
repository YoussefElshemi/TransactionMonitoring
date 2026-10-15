using Core.Enums;
using Core.Models.Rules;
using Core.Models.TransactionSchemas;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;

namespace Core.Helpers;

public static class VelocityHelper
{
    private static readonly VelocityFunction[] NumericFunctions =
    [
        VelocityFunction.Sum,
        VelocityFunction.Min,
        VelocityFunction.Max,
        VelocityFunction.Average,
        VelocityFunction.StructuringCount
    ];

    public static List<SchemaViolation> ValidateRuleBody(RuleBodyModel ruleBody, VelocityProfile? velocityProfile, TransactionSchema? transactionSchema)
    {
        var violations = new List<SchemaViolation>();

        Collect(ruleBody, velocityProfile, transactionSchema, violations);

        return violations;
    }

    public static (List<VelocityMetricName> Metrics, List<VelocityThresholdName> Thresholds) CollectNames(RuleBodyModel ruleBody)
    {
        var conditions = CollectConditions(ruleBody);

        var metrics = conditions
            .Where(x => !string.IsNullOrWhiteSpace(x.Metric))
            .Select(x => new VelocityMetricName(x.Metric!))
            .Distinct()
            .ToList();

        var thresholds = conditions
            .Where(x => !string.IsNullOrWhiteSpace(x.Threshold))
            .Select(x => new VelocityThresholdName(x.Threshold!))
            .Distinct()
            .ToList();

        return (metrics, thresholds);
    }

    public static List<VelocityConditionModel> CollectConditions(RuleBodyModel ruleBody)
    {
        var conditions = new List<VelocityConditionModel>();

        Collect(ruleBody, conditions);

        return conditions;
    }

    private static void Collect(RuleBodyModel rule, List<VelocityConditionModel> conditions)
    {
        if (rule.Velocity != null)
        {
            conditions.Add(rule.Velocity);
        }

        if (rule.Expression != null)
        {
            Collect(rule.Expression, conditions);
        }

        foreach (var child in (rule.And ?? []).Concat(rule.Or ?? []).Concat(rule.Not ?? []))
        {
            Collect(child, conditions);
        }
    }

    private static void Collect(RuleExpressionModel expression, List<VelocityConditionModel> conditions)
    {
        if (expression.Velocity != null)
        {
            conditions.Add(expression.Velocity);
        }

        foreach (var argument in expression.Arguments ?? [])
        {
            Collect(argument, conditions);
        }
    }

    private static void Collect(RuleBodyModel rule, VelocityProfile? velocityProfile, TransactionSchema? transactionSchema, List<SchemaViolation> violations)
    {
        if (rule.Velocity != null)
        {
            CollectLeafViolations(rule.Velocity, velocityProfile, transactionSchema, violations);
        }

        if (rule.Expression != null)
        {
            Collect(rule.Expression, velocityProfile, transactionSchema, violations);
        }

        foreach (var child in (rule.And ?? []).Concat(rule.Or ?? []).Concat(rule.Not ?? []))
        {
            Collect(child, velocityProfile, transactionSchema, violations);
        }
    }

    private static void Collect(RuleExpressionModel expression, VelocityProfile? velocityProfile, TransactionSchema? transactionSchema, List<SchemaViolation> violations)
    {
        if (expression.Velocity != null)
        {
            CollectLeafViolations(expression.Velocity, velocityProfile, transactionSchema, violations);
        }

        foreach (var argument in expression.Arguments ?? [])
        {
            Collect(argument, velocityProfile, transactionSchema, violations);
        }
    }

    private static void CollectLeafViolations(
        VelocityConditionModel velocity,
        VelocityProfile? velocityProfile,
        TransactionSchema? transactionSchema,
        List<SchemaViolation> violations)
    {
        if (velocityProfile == null)
        {
            violations.Add(Violation(velocity, "cannot be read, because the organisation has no velocity profile and so records nothing"));

            return;
        }

        var metric = velocityProfile.Metrics.FirstOrDefault(x => string.Equals(x.Name, velocity.Metric, StringComparison.OrdinalIgnoreCase));

        if (metric == null)
        {
            violations.Add(Violation(velocity, "is not a metric the organisation's velocity profile declares"));

            return;
        }

        CollectThresholdViolations(velocity, velocityProfile, violations);
        CollectTypeViolations(velocity, metric, transactionSchema, violations);
    }

    private static void CollectThresholdViolations(VelocityConditionModel velocity, VelocityProfile velocityProfile, List<SchemaViolation> violations)
    {
        if (velocity.Function != VelocityFunction.StructuringCount)
        {
            if (!string.IsNullOrWhiteSpace(velocity.Threshold) || velocity.Band.HasValue)
            {
                violations.Add(Violation(velocity, $"names a threshold, which only a '{VelocityFunction.StructuringCount}' reads"));
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(velocity.Threshold) || !velocity.Band.HasValue)
        {
            violations.Add(Violation(velocity, $"must name a Threshold and a Band, because a '{VelocityFunction.StructuringCount}' counts what sits just under a figure"));

            return;
        }

        if (!velocityProfile.Thresholds.Any(x => string.Equals(x.Name, velocity.Threshold, StringComparison.OrdinalIgnoreCase)))
        {
            violations.Add(Violation(velocity, $"names '{velocity.Threshold}', which is not a threshold the organisation's velocity profile declares"));
        }
    }

    private static void CollectTypeViolations(
        VelocityConditionModel velocity,
        VelocityMetric metric,
        TransactionSchema? transactionSchema,
        List<SchemaViolation> violations)
    {
        if (transactionSchema == null ||
            !NumericFunctions.Contains(velocity.Function) ||
            !TransactionSchemaHelper.TryResolveType(metric.Path, transactionSchema, out var type) ||
            type == SchemaFieldType.Number)
        {
            return;
        }

        violations.Add(Violation(velocity, $"reads '{metric.Path}', which is {TransactionSchemaHelper.DescribeType(type)}, so '{velocity.Function}' can never total anything"));
    }

    private static SchemaViolation Violation(VelocityConditionModel velocity, string message)
    {
        return new SchemaViolation
        {
            Field = VelocityFactsReader.Describe(velocity),
            Message = message
        };
    }
}
