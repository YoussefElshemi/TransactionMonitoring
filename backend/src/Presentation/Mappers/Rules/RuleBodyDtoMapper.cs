using Core.Enums;
using Core.Models.Rules;
using Core.ValueObjects;
using Presentation.Models.Rules;

namespace Presentation.Mappers.Rules;

public static class RuleBodyDtoMapper
{
    public static RuleBody ToModel(this RuleBodyDto ruleBodyDto)
    {
        return new RuleBody
        {
            Condition = !string.IsNullOrWhiteSpace(ruleBodyDto.Condition) ? Enum.Parse<RuleCondition>(ruleBodyDto.Condition, true) : null,
            Field = !string.IsNullOrWhiteSpace(ruleBodyDto.Field) ? new RuleField(ruleBodyDto.Field) : null,
            Quantifier = !string.IsNullOrWhiteSpace(ruleBodyDto.Quantifier) ? Enum.Parse<RuleQuantifier>(ruleBodyDto.Quantifier, true) : null,
            Expression = ruleBodyDto.Expression?.ToModel(),
            Party = ruleBodyDto.Party?.ToModel(),
            Velocity = ruleBodyDto.Velocity?.ToModel(),
            Value = ruleBodyDto.Value != null ? new RuleValue(ruleBodyDto.Value) : null,
            And = ruleBodyDto.And?.Select(x => x.ToModel()).ToList(),
            Or = ruleBodyDto.Or?.Select(x => x.ToModel()).ToList(),
            Not = ruleBodyDto.Not?.Select(x => x.ToModel()).ToList()
        };
    }

    public static RuleExpression ToModel(this RuleExpressionDto expressionDto)
    {
        return new RuleExpression
        {
            Field = !string.IsNullOrWhiteSpace(expressionDto.Field) ? new RuleField(expressionDto.Field) : null,
            Value = expressionDto.Value != null ? new RuleValue(expressionDto.Value) : null,
            Party = expressionDto.Party?.ToModel(),
            Velocity = expressionDto.Velocity?.ToModel(),
            Function = !string.IsNullOrWhiteSpace(expressionDto.Function) ? Enum.Parse<RuleFunction>(expressionDto.Function, true) : null,
            Arguments = expressionDto.Arguments?.Select(x => x.ToModel()).ToList()
        };
    }

    public static PartyCondition ToModel(this PartyConditionDto partyConditionDto)
    {
        return new PartyCondition
        {
            Role = Enum.Parse<PartyRole>(partyConditionDto.Role, true),
            Attribute = Enum.Parse<PartyAttribute>(partyConditionDto.Attribute, true)
        };
    }

    public static RuleBodyDto ToDto(this RuleBody ruleBody)
    {
        return new RuleBodyDto
        {
            Condition = ruleBody.Condition != null ? ruleBody.Condition.ToString() : null,
            Field = ruleBody.Field,
            Quantifier = ruleBody.Quantifier?.ToString(),
            Expression = ruleBody.Expression?.ToDto(),
            Party = ruleBody.Party?.ToDto(),
            Velocity = ruleBody.Velocity?.ToDto(),
            Value = ruleBody.Value.HasValue ? RuleValue.ToObject(ruleBody.Value.Value) : null,
            And = ruleBody.And?.Select(x => x.ToDto()).ToArray(),
            Or = ruleBody.Or?.Select(x => x.ToDto()).ToArray(),
            Not = ruleBody.Not?.Select(x => x.ToDto()).ToArray()
        };
    }

    public static RuleExpressionDto ToDto(this RuleExpression expression)
    {
        return new RuleExpressionDto
        {
            Field = expression.Field?.ToString(),
            Value = expression.Value.HasValue ? RuleValue.ToObject(expression.Value.Value) : null,
            Party = expression.Party?.ToDto(),
            Velocity = expression.Velocity?.ToDto(),
            Function = expression.Function?.ToString(),
            Arguments = expression.Arguments?.Select(x => x.ToDto()).ToArray()
        };
    }

    public static PartyConditionDto ToDto(this PartyCondition party)
    {
        return new PartyConditionDto
        {
            Role = party.Role.ToString(),
            Attribute = party.Attribute.ToString()
        };
    }

    public static VelocityCondition ToModel(this VelocityConditionDto velocityConditionDto)
    {
        return new VelocityCondition
        {
            Role = Enum.Parse<PartyRole>(velocityConditionDto.Role, true),
            Metric = new VelocityMetricName(velocityConditionDto.Metric),
            Function = Enum.Parse<VelocityFunction>(velocityConditionDto.Function, true),
            Window = velocityConditionDto.Window!.ToModel(),
            IncludeCurrent = new IncludeCurrent(velocityConditionDto.IncludeCurrent!.Value),
            Threshold = !string.IsNullOrWhiteSpace(velocityConditionDto.Threshold) ? new VelocityThresholdName(velocityConditionDto.Threshold) : null,
            Band = velocityConditionDto.Band.HasValue ? new StructuringBand(velocityConditionDto.Band.Value) : null
        };
    }

    public static VelocityWindow ToModel(this VelocityWindowDto velocityWindowDto)
    {
        return new VelocityWindow
        {
            Kind = Enum.Parse<VelocityWindowKind>(velocityWindowDto.Kind, true),
            Unit = Enum.Parse<VelocityWindowUnit>(velocityWindowDto.Unit, true),
            Length = velocityWindowDto.Length.HasValue ? new WindowLength(velocityWindowDto.Length.Value) : null,
            TimeZone = !string.IsNullOrWhiteSpace(velocityWindowDto.TimeZone) ? new WindowTimeZone(velocityWindowDto.TimeZone) : null
        };
    }

    public static VelocityConditionDto ToDto(this VelocityCondition velocity)
    {
        return new VelocityConditionDto
        {
            Role = velocity.Role.ToString(),
            Metric = velocity.Metric.ToString(),
            Function = velocity.Function.ToString(),
            Window = velocity.Window.ToDto(),
            IncludeCurrent = velocity.IncludeCurrent,
            Threshold = velocity.Threshold?.ToString(),
            Band = velocity.Band
        };
    }

    public static VelocityWindowDto ToDto(this VelocityWindow window)
    {
        return new VelocityWindowDto
        {
            Kind = window.Kind.ToString(),
            Unit = window.Unit.ToString(),
            Length = window.Length,
            TimeZone = window.TimeZone?.ToString()
        };
    }
}
