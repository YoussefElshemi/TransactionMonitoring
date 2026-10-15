using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Mappers;

public static class RuleBodyMapper
{
    public static RuleBodyModel ToModel(this RuleBody ruleBody)
    {
        return new RuleBodyModel
        {
            Condition = ruleBody.Condition,
            Field = ruleBody.Field,
            Quantifier = ruleBody.Quantifier,
            Expression = ruleBody.Expression?.ToModel(),
            Party = ruleBody.Party?.ToModel(),
            Velocity = ruleBody.Velocity?.ToModel(),
            Value = ruleBody.Value.HasValue ? RuleValue.ToObject(ruleBody.Value.Value) : null,
            And = ruleBody.And?.Select(x => x.ToModel()).ToArray(),
            Or = ruleBody.Or?.Select(x => x.ToModel()).ToArray(),
            Not = ruleBody.Not?.Select(x => x.ToModel()).ToArray()
        };
    }

    public static RuleExpressionModel ToModel(this RuleExpression expression)
    {
        return new RuleExpressionModel
        {
            Field = expression.Field?.ToString(),
            Value = expression.Value.HasValue ? RuleValue.ToObject(expression.Value.Value) : null,
            Party = expression.Party?.ToModel(),
            Velocity = expression.Velocity?.ToModel(),
            Function = expression.Function,
            Arguments = expression.Arguments?.Select(x => x.ToModel()).ToArray()
        };
    }

    public static PartyConditionModel ToModel(this PartyCondition party)
    {
        return new PartyConditionModel
        {
            Role = party.Role,
            Attribute = party.Attribute
        };
    }

    public static VelocityConditionModel ToModel(this VelocityCondition velocity)
    {
        return new VelocityConditionModel
        {
            Role = velocity.Role,
            Metric = velocity.Metric,
            Function = velocity.Function,
            Window = velocity.Window.ToModel(),
            IncludeCurrent = velocity.IncludeCurrent,
            Threshold = velocity.Threshold?.ToString(),
            Band = velocity.Band
        };
    }

    public static VelocityWindowModel ToModel(this VelocityWindow window)
    {
        return new VelocityWindowModel
        {
            Kind = window.Kind,
            Unit = window.Unit,
            Length = window.Length,
            TimeZone = window.TimeZone?.ToString()
        };
    }
}
