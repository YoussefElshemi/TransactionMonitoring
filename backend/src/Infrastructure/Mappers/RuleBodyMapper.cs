using Core.Extensions;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Infrastructure.Mappers;

public static class RuleBodyMapper
{
    public static RuleBody ToModel(this RuleBodyModel ruleBody)
    {
        return new RuleBody
        {
            Condition = ruleBody.Condition,
            Field = ruleBody.Field != null ? new RuleField(ruleBody.Field) : null,
            Quantifier = ruleBody.Quantifier,
            Expression = ruleBody.Expression?.ToModel(),
            Party = ruleBody.Party?.ToModel(),
            Velocity = ruleBody.Velocity?.ToModel(),
            Value = ruleBody.Value != null ? new RuleValue(ruleBody.Value) : null,
            And = ruleBody.And?.Select(x => x.ToModel()).ToList(),
            Or = ruleBody.Or?.Select(x => x.ToModel()).ToList(),
            Not = ruleBody.Not?.Select(x => x.ToModel()).ToList()
        };
    }

    private static RuleExpression ToModel(this RuleExpressionModel expression)
    {
        return new RuleExpression
        {
            Field = expression.Field != null ? new RuleField(expression.Field) : null,
            Value = expression.Value != null ? new RuleValue(expression.Value) : null,
            Party = expression.Party?.ToModel(),
            Velocity = expression.Velocity?.ToModel(),
            Function = expression.Function,
            Arguments = expression.Arguments?.Select(x => x.ToModel()).ToList()
        };
    }

    private static PartyCondition ToModel(this PartyConditionModel party)
    {
        return new PartyCondition
        {
            Role = party.Role,
            Attribute = party.Attribute
        };
    }

    private static VelocityCondition ToModel(this VelocityConditionModel velocity)
    {
        return new VelocityCondition
        {
            Role = velocity.Role,
            Metric = new VelocityMetricName(velocity.Metric ?? string.Empty),
            Function = velocity.Function,
            Window = (velocity.Window ?? new VelocityWindowModel()).ToModel(),
            IncludeCurrent = new IncludeCurrent(velocity.IncludeCurrent),
            Threshold = !string.IsNullOrWhiteSpace(velocity.Threshold) ? new VelocityThresholdName(velocity.Threshold) : null,
            Band = velocity.Band.HasValue ? new StructuringBand(velocity.Band.Value) : null
        };
    }

    private static VelocityWindow ToModel(this VelocityWindowModel window)
    {
        return new VelocityWindow
        {
            Kind = window.Kind,
            Unit = window.Unit,
            Length = window.Length.HasValue ? new WindowLength(window.Length.Value) : null,
            TimeZone = !string.IsNullOrWhiteSpace(window.TimeZone) ? new WindowTimeZone(window.TimeZone) : null
        };
    }

    private static RuleBodyModel ToEntity(this RuleBody ruleBody)
    {
        return new RuleBodyModel
        {
            Condition = ruleBody.Condition,
            Field = ruleBody.Field,
            Quantifier = ruleBody.Quantifier,
            Expression = ruleBody.Expression?.ToEntity(),
            Party = ruleBody.Party?.ToEntity(),
            Velocity = ruleBody.Velocity?.ToEntity(),
            Value = ruleBody.Value.HasValue ? RuleValue.ToObject(ruleBody.Value.Value) : null,
            And = ruleBody.And?.Select(x => x.ToEntity()).ToArray(),
            Or = ruleBody.Or?.Select(x => x.ToEntity()).ToArray(),
            Not = ruleBody.Not?.Select(x => x.ToEntity()).ToArray()
        };
    }

    private static RuleExpressionModel ToEntity(this RuleExpression expression)
    {
        return new RuleExpressionModel
        {
            Field = expression.Field?.ToString(),
            Value = expression.Value.HasValue ? RuleValue.ToObject(expression.Value.Value) : null,
            Party = expression.Party?.ToEntity(),
            Velocity = expression.Velocity?.ToEntity(),
            Function = expression.Function,
            Arguments = expression.Arguments?.Select(x => x.ToEntity()).ToArray()
        };
    }

    private static PartyConditionModel ToEntity(this PartyCondition party)
    {
        return new PartyConditionModel
        {
            Role = party.Role,
            Attribute = party.Attribute
        };
    }

    private static VelocityConditionModel ToEntity(this VelocityCondition velocity)
    {
        return new VelocityConditionModel
        {
            Role = velocity.Role,
            Metric = velocity.Metric,
            Function = velocity.Function,
            Window = velocity.Window.ToEntity(),
            IncludeCurrent = velocity.IncludeCurrent,
            Threshold = velocity.Threshold?.ToString(),
            Band = velocity.Band
        };
    }

    private static VelocityWindowModel ToEntity(this VelocityWindow window)
    {
        return new VelocityWindowModel
        {
            Kind = window.Kind,
            Unit = window.Unit,
            Length = window.Length,
            TimeZone = window.TimeZone?.ToString()
        };
    }

    public static string ToBase64String(this RuleBody ruleBody)
    {
        return ruleBody.ToEntity().Serialize().ToBase64String();
    }
}
