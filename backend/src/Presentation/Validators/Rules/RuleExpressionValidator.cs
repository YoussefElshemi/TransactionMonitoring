using Core.Enums;
using Core.Helpers;
using FluentValidation;
using Presentation.Models.Rules;

namespace Presentation.Validators.Rules;

public class RuleExpressionValidator : AbstractValidator<RuleExpressionDto>
{
    public RuleExpressionValidator()
    {
        RuleFor(x => x)
            .Cascade(CascadeMode.Stop)
            .Must(x => Depth(x) <= RuleExpressionEvaluator.MaxDepth)
            .WithMessage($"An expression cannot be nested more than {RuleExpressionEvaluator.MaxDepth} deep.")
            .DependentRules(() =>
            {
                RuleFor(x => x)
                    .Must(BeExactlyOneForm)
                    .WithMessage("An expression must be a field, a party, a velocity window, a value or a function, and only one of the five.");

                RuleFor(x => x.Function)
                    .IsEnumName(typeof(RuleFunction), false)
                    .When(x => !string.IsNullOrWhiteSpace(x.Function));

                RuleFor(x => x.Party!)
                    .SetValidator(new PartyConditionValidator())
                    .When(x => x.Party != null);

                RuleFor(x => x.Velocity!)
                    .SetValidator(new VelocityConditionValidator())
                    .When(x => x.Velocity != null);

                RuleFor(x => x)
                    .Custom(ValidateExpression);

                RuleForEach(x => x.Arguments)
                    .SetValidator(this)
                    .When(x => x.Arguments != null);
            });
    }

    private static bool BeExactlyOneForm(RuleExpressionDto expression)
    {
        var forms = 0;

        if (!string.IsNullOrWhiteSpace(expression.Field)) forms++;
        if (expression.Party != null) forms++;
        if (expression.Velocity != null) forms++;
        if (expression.Value != null) forms++;
        if (!string.IsNullOrWhiteSpace(expression.Function)) forms++;

        return forms == 1;
    }

    private static void ValidateExpression(RuleExpressionDto expression, ValidationContext<RuleExpressionDto> context)
    {
        if (!string.IsNullOrWhiteSpace(expression.Field))
        {
            if (!PayloadPath.TryParse(expression.Field, out _))
            {
                context.AddFailure($"'{expression.Field}' is not a readable field path.");
            }

            if (expression.Arguments is { Length: > 0 })
            {
                context.AddFailure("A field expression cannot take arguments.");
            }

            return;
        }

        if (expression.Party != null)
        {
            if (expression.Arguments is { Length: > 0 })
            {
                context.AddFailure("A party expression cannot take arguments.");
            }

            return;
        }

        if (expression.Velocity != null)
        {
            if (expression.Arguments is { Length: > 0 })
            {
                context.AddFailure("A velocity expression cannot take arguments.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(expression.Function) || !Enum.TryParse<RuleFunction>(expression.Function, true, out var function))
        {
            if (expression.Arguments is { Length: > 0 })
            {
                context.AddFailure("A value expression cannot take arguments.");
            }

            if (expression.Value != null && !IsSingleValue(expression.Value))
            {
                context.AddFailure("A value expression must be a single string, number or boolean.");
            }

            return;
        }

        var arguments = expression.Arguments ?? [];

        if (arguments.Length != RuleExpressionEvaluator.Arity(function))
        {
            context.AddFailure($"'{function}' takes {RuleExpressionEvaluator.Arity(function)} argument(s) but was given {arguments.Length}.");

            return;
        }

        var aggregate = RuleExpressionEvaluator.IsAggregate(function);

        foreach (var argument in arguments)
        {
            var wildcard = !string.IsNullOrWhiteSpace(argument.Field) && PayloadPath.HasWildcard(argument.Field);

            if (aggregate && !wildcard && argument.Party == null && argument.Velocity == null)
            {
                context.AddFailure($"'{function}' reduces many values, so its argument must be a field path carrying '[*]', a party role or a velocity window.");
            }

            if (!aggregate && wildcard)
            {
                context.AddFailure($"'{function}' reads a single value, so its argument must not be a field path carrying '[*]'.");
            }
        }
    }

    private static bool IsSingleValue(object value)
    {
        var literal = RuleValueHelper.Extract(value);

        return literal is string or bool || RuleValueHelper.TryGetNumber(literal, out _);
    }

    private static int Depth(RuleExpressionDto? expression)
    {
        if (expression == null)
        {
            return 0;
        }

        var arguments = expression.Arguments ?? [];

        return 1 + (arguments.Length == 0 ? 0 : arguments.Max(Depth));
    }
}
