using System.Globalization;
using System.Text.Json.Nodes;
using Core.Enums;
using Core.Models.Parties;
using Core.Models.Rules;
using Core.Models.VelocityObservations;

namespace Core.Helpers;

public static class RuleExpressionEvaluator
{
    public const int MaxDepth = 5;

    private static readonly RuleFunction[] AggregateFunctions =
    [
        RuleFunction.Count,
        RuleFunction.Sum,
        RuleFunction.Min,
        RuleFunction.Max,
        RuleFunction.Average
    ];

    private static readonly RuleFunction[] TemporalFunctions =
    [
        RuleFunction.HourOfDay,
        RuleFunction.MinuteOfDay,
        RuleFunction.DayOfWeek,
        RuleFunction.DaysSince,
        RuleFunction.HoursSince
    ];

    public static bool IsAggregate(RuleFunction function)
    {
        return AggregateFunctions.Contains(function);
    }

    public static bool IsTemporal(RuleFunction function)
    {
        return TemporalFunctions.Contains(function);
    }

    public static int Arity(RuleFunction function)
    {
        return function is RuleFunction.Add or RuleFunction.Subtract or RuleFunction.Multiply or RuleFunction.Divide or RuleFunction.Modulo ? 2 : 1;
    }

    public static object? Evaluate(RuleExpressionModel expression, JsonObject payload, DateTimeOffset now, IReadOnlyList<PartyFacts>? parties = null, VelocityFacts? velocity = null)
    {
        if (expression.Field != null)
        {
            return RuleValueHelper.Extract(PayloadPath.ResolveSingle(payload, expression.Field));
        }

        if (expression.Party != null)
        {
            return PartyFactsReader.ReadSingle(expression.Party, parties, now);
        }

        if (expression.Velocity != null)
        {
            return VelocityFactsReader.ReadSingle(expression.Velocity, parties, velocity, now);
        }

        if (expression.Function == null)
        {
            return RuleValueHelper.Extract(expression.Value);
        }

        var function = expression.Function.Value;
        var arguments = expression.Arguments ?? [];

        if (arguments.Length != Arity(function))
        {
            return null;
        }

        if (IsAggregate(function))
        {
            return Aggregate(function, EvaluateAll(arguments[0], payload, now, parties, velocity));
        }

        if (IsTemporal(function))
        {
            return Temporal(function, Evaluate(arguments[0], payload, now, parties, velocity), now);
        }

        return function switch
        {
            RuleFunction.Abs => Unary(Evaluate(arguments[0], payload, now, parties, velocity), Math.Abs),
            RuleFunction.Length => Evaluate(arguments[0], payload, now, parties, velocity) is string text ? (decimal)text.Length : null,
            _ => Arithmetic(function, Evaluate(arguments[0], payload, now, parties, velocity), Evaluate(arguments[1], payload, now, parties, velocity))
        };
    }

    public static List<object?> EvaluateAll(RuleExpressionModel expression, JsonObject payload, DateTimeOffset now, IReadOnlyList<PartyFacts>? parties = null, VelocityFacts? velocity = null)
    {
        if (expression.Field != null)
        {
            return PayloadPath.ResolveAll(payload, expression.Field).ConvertAll(RuleValueHelper.Extract);
        }

        if (expression.Party != null)
        {
            return PartyFactsReader.ReadAll(expression.Party, parties, now);
        }

        if (expression.Velocity != null)
        {
            return VelocityFactsReader.ReadAll(expression.Velocity, parties, velocity, now);
        }

        return [Evaluate(expression, payload, now, parties, velocity)];
    }

    public static string Describe(RuleExpressionModel expression)
    {
        if (expression.Field != null)
        {
            return expression.Field;
        }

        if (expression.Party != null)
        {
            return PartyFactsReader.Describe(expression.Party);
        }

        if (expression.Velocity != null)
        {
            return VelocityFactsReader.Describe(expression.Velocity);
        }

        if (expression.Function == null)
        {
            return Convert.ToString(RuleValueHelper.Extract(expression.Value), CultureInfo.InvariantCulture) ?? "null";
        }

        var name = char.ToLowerInvariant(expression.Function.Value.ToString()[0]) + expression.Function.Value.ToString()[1..];

        return $"{name}({string.Join(", ", (expression.Arguments ?? []).Select(Describe))})";
    }

    public static int Depth(RuleExpressionModel? expression)
    {
        if (expression == null)
        {
            return 0;
        }

        var arguments = expression.Arguments ?? [];

        return 1 + (arguments.Length == 0 ? 0 : arguments.Max(Depth));
    }

    private static object? Aggregate(RuleFunction function, List<object?> values)
    {
        var present = values.Where(x => x != null).ToList();

        if (function == RuleFunction.Count)
        {
            return (decimal)present.Count;
        }

        var numbers = new List<decimal>();

        foreach (var value in present)
        {
            if (RuleValueHelper.TryGetNumber(value, out var number))
            {
                numbers.Add(number);
            }
        }

        if (numbers.Count == 0)
        {
            return null;
        }

        try
        {
            return function switch
            {
                RuleFunction.Sum => numbers.Sum(),
                RuleFunction.Min => numbers.Min(),
                RuleFunction.Max => numbers.Max(),
                RuleFunction.Average => numbers.Average(),
                _ => null
            };
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private static object? Temporal(RuleFunction function, object? value, DateTimeOffset now)
    {
        if (!RuleValueHelper.TryGetDateTime(value, out var timestamp))
        {
            return null;
        }

        return function switch
        {
            RuleFunction.HourOfDay => (decimal)timestamp.Hour,
            RuleFunction.MinuteOfDay => (decimal)(timestamp.Hour * 60 + timestamp.Minute),
            // ISO-8601: Monday is 1, Sunday is 7.
            RuleFunction.DayOfWeek => (decimal)(timestamp.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)timestamp.DayOfWeek),
            RuleFunction.DaysSince => (decimal)(now - timestamp).TotalDays,
            RuleFunction.HoursSince => (decimal)(now - timestamp).TotalHours,
            _ => null
        };
    }

    private static object? Unary(object? value, Func<decimal, decimal> operation)
    {
        if (!RuleValueHelper.TryGetNumber(value, out var number))
        {
            return null;
        }

        try
        {
            return operation(number);
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private static object? Arithmetic(RuleFunction function, object? left, object? right)
    {
        if (!RuleValueHelper.TryGetNumber(left, out var number1) || !RuleValueHelper.TryGetNumber(right, out var number2))
        {
            return null;
        }

        if (function is RuleFunction.Divide or RuleFunction.Modulo && number2 == 0)
        {
            return null;
        }

        try
        {
            return function switch
            {
                RuleFunction.Add => number1 + number2,
                RuleFunction.Subtract => number1 - number2,
                RuleFunction.Multiply => number1 * number2,
                RuleFunction.Divide => number1 / number2,
                RuleFunction.Modulo => number1 % number2,
                _ => null
            };
        }
        catch (OverflowException)
        {
            return null;
        }
    }
}
