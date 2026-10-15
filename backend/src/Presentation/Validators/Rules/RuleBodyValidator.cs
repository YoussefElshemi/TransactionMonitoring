using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using Core.Enums;
using Core.Helpers;
using Core.Models.Rules;
using FluentValidation;
using Presentation.Models.Rules;

namespace Presentation.Validators.Rules;

public class RuleBodyValidator : AbstractValidator<RuleBodyDto>
{
    internal const int MaxDepth = 7;
    internal const int MaxRulesInCollection = 10;
    internal const int MaxValuesInList = 1000;

    private static readonly RuleCondition[] OrderingConditions =
    [
        RuleCondition.GreaterThan,
        RuleCondition.LessThan,
        RuleCondition.GreaterThanEqual,
        RuleCondition.LessThanEqual
    ];

    private static readonly RuleCondition[] ListConditions =
    [
        RuleCondition.In,
        RuleCondition.NotIn
    ];

    private static readonly RuleCondition[] StringConditions =
    [
        RuleCondition.Contains,
        RuleCondition.StartsWith,
        RuleCondition.EndsWith,
        RuleCondition.Matches
    ];

    public RuleBodyValidator()
    {
        RuleFor(x => x)
            .Cascade(CascadeMode.Stop)
            .Must(NotExceedMaxDepth)
            .WithMessage($"{nameof(Rule)} depth cannot be greater than {MaxDepth}")
            .DependentRules(() =>
            {
                RuleFor(x => x)
                    .Must(BeEitherBasicOrComposite)
                    .WithMessage($"{nameof(Rule)} must either be a basic rule (Condition with a Field or an Expression, and a Value) or one of And, Or, Not, but not both.");

                RuleFor(x => x.And)
                    .Cascade(CascadeMode.Stop)
                    .Must(ValidateMaxCompositeRuleSize)
                    .WithMessage($"The 'And' collection must not contain more than {MaxRulesInCollection} rules.")
                    .Must(ValidateMinCompositeRuleSize)
                    .WithMessage("The 'And' collection must contain a rule.")
                    .DependentRules(() => { RuleForEach(x => x.And).SetValidator(this); });

                RuleFor(x => x.Or)
                    .Cascade(CascadeMode.Stop)
                    .Must(ValidateMaxCompositeRuleSize)
                    .WithMessage($"The 'Or' collection must not contain more than {MaxRulesInCollection} rules.")
                    .Must(ValidateMinCompositeRuleSize)
                    .WithMessage("The 'Or' collection must contain a rule.")
                    .DependentRules(() => { RuleForEach(x => x.Or).SetValidator(this); });

                When(IsBasicRule, () =>
                {
                    RuleFor(x => x.Condition)
                        .IsEnumName(typeof(RuleCondition), false);

                    RuleFor(x => x.Quantifier)
                        .IsEnumName(typeof(RuleQuantifier), false)
                        .When(x => !string.IsNullOrWhiteSpace(x.Quantifier));

                    RuleFor(x => x.Field)
                        .NotEmpty()
                        .WithMessage("Field must be provided for a basic rule.")
                        .When(x => x.Expression == null && x.Party == null && x.Velocity == null);

                    RuleFor(x => x.Party!)
                        .SetValidator(new PartyConditionValidator())
                        .When(x => x.Party != null);

                    RuleFor(x => x.Velocity!)
                        .SetValidator(new VelocityConditionValidator())
                        .When(x => x.Velocity != null);

                    RuleFor(x => x.Value)
                        .NotNull()
                        .WithMessage("Value must be provided for a basic rule.")
                        .When(x => x.Condition != null && !x.Condition.Equals(RuleCondition.IsNull.ToString(), StringComparison.OrdinalIgnoreCase));

                    RuleFor(x => x.Expression!)
                        .SetValidator(new RuleExpressionValidator())
                        .When(x => x.Expression != null);

                    RuleFor(x => x)
                        .Custom(ValidateBasicRule);
                });

                When(x => x.Not != null, () =>
                {
                    RuleFor(x => x.Not)
                        .Cascade(CascadeMode.Stop)
                        .Must(x => x!.Length == 1)
                        .WithMessage("The 'Not' collection must contain only one element.")
                        .DependentRules(() => { RuleForEach(x => x.Not).SetValidator(this); });
                });
            });
    }

    private static bool BeEitherBasicOrComposite(RuleBodyDto dto)
    {
        var isBasic = IsBasicRule(dto);
        var isComposite = IsCompositeRule(dto);

        return isBasic ^ isComposite;
    }

    private static bool IsBasicRule(RuleBodyDto dto)
    {
        var isConditionIsNull = dto.Condition != null && dto.Condition.Equals(RuleCondition.IsNull.ToString(), StringComparison.OrdinalIgnoreCase);
        var addressesAValue = !string.IsNullOrWhiteSpace(dto.Field) || dto.Expression != null || dto.Party != null || dto.Velocity != null;

        return !string.IsNullOrWhiteSpace(dto.Condition) &&
               addressesAValue &&
               (isConditionIsNull || dto.Value != null) &&
               dto.And == null &&
               dto.Or == null &&
               dto.Not == null;
    }

    private static bool IsCompositeRule(RuleBodyDto dto)
    {
        return (dto.And != null || dto.Or != null || dto.Not != null) &&
               string.IsNullOrWhiteSpace(dto.Condition) &&
               string.IsNullOrWhiteSpace(dto.Field) &&
               string.IsNullOrWhiteSpace(dto.Quantifier) &&
               dto.Expression == null &&
               dto.Party == null &&
               dto.Velocity == null &&
               dto.Value == null;
    }

    private static int CalculateDepth(RuleBodyDto? dto)
    {
        if (dto == null) return 0;

        var andDepth = dto.And != null && dto.And.Length != 0 ? dto.And.Max(CalculateDepth) : 0;
        var orDepth = dto.Or != null && dto.Or.Length != 0 ? dto.Or.Max(CalculateDepth) : 0;
        var notDepth = dto.Not != null && dto.Not.Length != 0 ? dto.Not.Max(CalculateDepth) : 0;

        return 1 + Math.Max(andDepth, Math.Max(orDepth, notDepth));
    }

    private static bool NotExceedMaxDepth(RuleBodyDto dto)
    {
        return CalculateDepth(dto) <= MaxDepth;
    }

    private static bool ValidateMaxCompositeRuleSize(RuleBodyDto[]? collection)
    {
        if (collection == null)
        {
            return true;
        }

        return collection.Length <= MaxRulesInCollection;
    }

    private static bool ValidateMinCompositeRuleSize(RuleBodyDto[]? collection)
    {
        if (collection == null)
        {
            return true;
        }

        return collection.Length != 0;
    }

    private static void ValidateBasicRule(RuleBodyDto rule, ValidationContext<RuleBodyDto> context)
    {
        if (rule.Condition == null || !Enum.TryParse<RuleCondition>(rule.Condition, true, out var condition))
        {
            return;
        }

        ValidateSubject(rule, context);
        ValidateValue(rule, condition, context);
    }

    private static void ValidateSubject(RuleBodyDto rule, ValidationContext<RuleBodyDto> context)
    {
        var subjects = (string.IsNullOrWhiteSpace(rule.Field) ? 0 : 1) +
                       (rule.Expression == null ? 0 : 1) +
                       (rule.Party == null ? 0 : 1) +
                       (rule.Velocity == null ? 0 : 1);

        if (subjects > 1)
        {
            context.AddFailure("A rule must address exactly one of a Field, an Expression, a Party or a Velocity window.");

            return;
        }

        if (rule.Party != null || rule.Velocity != null)
        {
            return;
        }

        if (rule.Expression != null)
        {
            if (!string.IsNullOrWhiteSpace(rule.Quantifier))
            {
                context.AddFailure("A quantifier can only be used on a field path, not on an expression.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(rule.Field) || !PayloadPath.IsAddressable(rule.Field))
        {
            context.AddFailure($"'{rule.Field}' is not a readable field path.");

            return;
        }

        var wildcard = PayloadPath.HasWildcard(rule.Field);
        var quantified = !string.IsNullOrWhiteSpace(rule.Quantifier);

        if (wildcard && !quantified)
        {
            context.AddFailure("A field path carrying '[*]' must state a Quantifier of 'Any' or 'All'.");
        }

        if (!wildcard && quantified)
        {
            context.AddFailure("A Quantifier can only be used on a field path carrying '[*]'.");
        }
    }

    private static void ValidateValue(RuleBodyDto rule, RuleCondition condition, ValidationContext<RuleBodyDto> context)
    {
        if (condition == RuleCondition.IsNull)
        {
            if (rule.Value != null)
            {
                context.AddFailure("Value must be not set when the condition is 'IsNull'.");
            }

            return;
        }

        if (rule.Value == null)
        {
            return;
        }

        if (!TryGetKind(rule.Value, out var kind) || kind is JsonValueKind.Object)
        {
            context.AddFailure($"Value must be a single value or a list of values when the condition is '{condition}'.");

            return;
        }

        if (OrderingConditions.Contains(condition))
        {
            // String bounds compare lexicographically, which orders ISO-8601 timestamps correctly.
            if (kind is not (JsonValueKind.Number or JsonValueKind.String))
            {
                context.AddFailure($"Value must be a number or a string when the condition is '{condition}'.");
            }

            return;
        }

        if (ListConditions.Contains(condition))
        {
            ValidateList(rule.Value, condition, context);

            return;
        }

        if (condition == RuleCondition.Between)
        {
            ValidateBounds(rule.Value, context);

            return;
        }

        if (StringConditions.Contains(condition))
        {
            if (kind != JsonValueKind.String)
            {
                context.AddFailure($"Value must be a string when the condition is '{condition}'.");

                return;
            }

            if (condition == RuleCondition.Matches)
            {
                ValidatePattern(rule.Value, context);
            }

            return;
        }

        if (kind == JsonValueKind.Array)
        {
            context.AddFailure($"Value must be a single value when the condition is '{condition}'.");
        }
    }

    private static void ValidateList(object? value, RuleCondition condition, ValidationContext<RuleBodyDto> context)
    {
        if (!TryGetElements(value, out var elements))
        {
            context.AddFailure($"Value must be a list when the condition is '{condition}'.");

            return;
        }

        if (elements.Count == 0)
        {
            context.AddFailure($"Value must not be an empty list when the condition is '{condition}'.");

            return;
        }

        if (elements.Count > MaxValuesInList)
        {
            context.AddFailure($"Value must not hold more than {MaxValuesInList} values when the condition is '{condition}'.");

            return;
        }

        if (elements.Any(x => !TryGetKind(x, out var kind) || kind is JsonValueKind.Array or JsonValueKind.Object))
        {
            context.AddFailure($"Value must be a list of single values when the condition is '{condition}'.");
        }
    }

    private static void ValidateBounds(object? value, ValidationContext<RuleBodyDto> context)
    {
        if (!TryGetElements(value, out var elements) || elements.Count != 2)
        {
            context.AddFailure("Value must be a list of exactly two bounds when the condition is 'Between'.");

            return;
        }

        var lower = RuleValueHelper.Extract(elements[0]);
        var upper = RuleValueHelper.Extract(elements[1]);
        var comparison = RuleValueHelper.Compare(lower, upper);

        if (comparison == null)
        {
            context.AddFailure("The 'Between' bounds must be two numbers or two strings.");

            return;
        }

        if (comparison > 0)
        {
            context.AddFailure("The 'Between' bounds must be given lower first.");
        }
    }

    private static void ValidatePattern(object? value, ValidationContext<RuleBodyDto> context)
    {
        var pattern = RuleValueHelper.Extract(value) as string ?? string.Empty;

        if (pattern.Length > RuleHelper.MaxPatternLength)
        {
            context.AddFailure($"Value must not be longer than {RuleHelper.MaxPatternLength} characters when the condition is 'Matches'.");

            return;
        }

        if (!RuleHelper.TryCompilePattern(pattern))
        {
            context.AddFailure("Value must be a regular expression the engine can run when the condition is 'Matches'.");
        }
    }

    private static bool TryGetKind(object? value, out JsonValueKind kind)
    {
        switch (value)
        {
            case null:
                kind = JsonValueKind.Null;
                return true;
            case JsonElement jsonElement:
                kind = jsonElement.ValueKind;
                return true;
            case JsonNode jsonNode:
                kind = jsonNode.GetValueKind();
                return true;
            case string:
                kind = JsonValueKind.String;
                return true;
            case bool boolean:
                kind = boolean ? JsonValueKind.True : JsonValueKind.False;
                return true;
            case decimal or double or float or long or int or short or byte:
                kind = JsonValueKind.Number;
                return true;
            case IEnumerable:
                kind = JsonValueKind.Array;
                return true;
            default:
                kind = JsonValueKind.Undefined;
                return false;
        }
    }

    private static bool TryGetElements(object? value, out List<object?> elements)
    {
        switch (value)
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
}
