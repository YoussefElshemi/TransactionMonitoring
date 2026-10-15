using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Core.Enums;
using Core.Models.Parties;
using Core.Models.RuleEvaluations;
using Core.Models.Rules;
using Core.Models.VelocityObservations;
using Core.ValueObjects;

namespace Core.Helpers;

public static class RuleHelper
{
    public const int MaxPatternLength = 512;

    // NonBacktracking removes catastrophic backtracking on user-supplied patterns rather than bounding it with a timeout.
    public const RegexOptions PatternOptions = RegexOptions.NonBacktracking | RegexOptions.CultureInvariant;

    // The static Regex cache holds only 15 patterns, so compiled engines are cached here; the ceiling just bounds growth.
    private const int MaxCachedPatterns = 512;

    private static readonly ConcurrentDictionary<string, Regex> CompiledPatterns = new(StringComparer.Ordinal);

    public static bool ContainsVelocity(RuleBody? ruleBody)
    {
        if (ruleBody is null)
        {
            return false;
        }

        return ruleBody.Velocity is not null ||
               ContainsVelocity(ruleBody.And) ||
               ContainsVelocity(ruleBody.Or) ||
               ContainsVelocity(ruleBody.Not);
    }

    private static bool ContainsVelocity(List<RuleBody>? ruleBodies)
    {
        return ruleBodies is not null && ruleBodies.Exists(ContainsVelocity);
    }

    public static Matched Matches(RuleBodyModel rule, JsonObject payload, RuleMatchPolarity polarity, DateTimeOffset now, IReadOnlyList<PartyFacts>? parties = null, VelocityFacts? velocity = null)
    {
        var result = Evaluate(rule, payload, now, parties, velocity);

        return new Matched(polarity == RuleMatchPolarity.MatchesWhenTrue ? result : !result);
    }

    public static List<ConditionMatch> Trace(RuleBodyModel rule, JsonObject payload, RuleMatchPolarity polarity, DateTimeOffset now, IReadOnlyList<PartyFacts>? parties = null, VelocityFacts? velocity = null)
    {
        var conditionMatches = new List<ConditionMatch>();

        Collect(rule, payload, polarity == RuleMatchPolarity.MatchesWhenFalse, now, parties, velocity, conditionMatches);

        return conditionMatches;
    }

    public static bool Evaluate(RuleBodyModel rule, JsonObject payload, DateTimeOffset now, IReadOnlyList<PartyFacts>? parties = null, VelocityFacts? velocity = null)
    {
        if (rule.Not is { Length: > 0 })
        {
            return !Evaluate(rule.Not.First(), payload, now, parties, velocity);
        }

        if (rule.Field != null || rule.Expression != null || rule.Party != null || rule.Velocity != null)
        {
            return EvaluateLeaf(rule, payload, now, parties, velocity);
        }

        if (rule.And != null)
        {
            return rule.And.All(r => Evaluate(r, payload, now, parties, velocity));
        }

        if (rule.Or != null)
        {
            return rule.Or.Any(r => Evaluate(r, payload, now, parties, velocity));
        }

        return false;
    }

    public static bool TryCompilePattern(string pattern)
    {
        return Compile(pattern) != null;
    }

    private static Regex? Compile(string pattern)
    {
        if (CompiledPatterns.TryGetValue(pattern, out var cached))
        {
            return cached;
        }

        Regex regex;

        try
        {
            regex = new Regex(pattern, PatternOptions);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }

        if (CompiledPatterns.Count < MaxCachedPatterns)
        {
            CompiledPatterns.TryAdd(pattern, regex);
        }

        return regex;
    }

    private static bool EvaluateLeaf(RuleBodyModel rule, JsonObject payload, DateTimeOffset now, IReadOnlyList<PartyFacts>? parties, VelocityFacts? velocity)
    {
        var ruleValue = RuleValueHelper.Extract(rule.Value);

        if (rule.Expression != null)
        {
            return Test(rule.Condition, RuleExpressionEvaluator.Evaluate(rule.Expression, payload, now, parties, velocity), ruleValue);
        }

        if (rule.Party != null)
        {
            if (rule.Quantifier == null)
            {
                return Test(rule.Condition, PartyFactsReader.ReadSingle(rule.Party, parties, now), ruleValue);
            }

            return Quantify(rule, PartyFactsReader.ReadAll(rule.Party, parties, now), ruleValue);
        }

        if (rule.Velocity != null)
        {
            if (rule.Quantifier == null)
            {
                return Test(rule.Condition, VelocityFactsReader.ReadSingle(rule.Velocity, parties, velocity, now), ruleValue);
            }

            return Quantify(rule, VelocityFactsReader.ReadAll(rule.Velocity, parties, velocity, now), ruleValue);
        }

        if (rule.Quantifier == null)
        {
            return Test(rule.Condition, RuleValueHelper.Extract(PayloadPath.ResolveSingle(payload, rule.Field!)), ruleValue);
        }

        return Quantify(rule, ResolveMany(rule, payload), ruleValue);
    }

    private static bool Quantify(RuleBodyModel rule, List<object?> values, object? ruleValue)
    {
        if (values.Count == 0)
        {
            return false;
        }

        return rule.Quantifier == RuleQuantifier.Any
            ? values.Any(x => Test(rule.Condition, x, ruleValue))
            : values.All(x => Test(rule.Condition, x, ruleValue));
    }

    private static List<object?> ResolveMany(RuleBodyModel rule, JsonObject payload)
    {
        return PayloadPath.ResolveAll(payload, rule.Field!).ConvertAll(RuleValueHelper.Extract);
    }

    // Missing values only satisfy IsNull; negative operators answer false too, so a missing field never fires a rule alone.
    private static bool Test(RuleCondition? condition, object? fieldValue, object? ruleValue)
    {
        if (condition == RuleCondition.IsNull)
        {
            return fieldValue == null;
        }

        if (fieldValue == null)
        {
            return false;
        }

        return condition switch
        {
            RuleCondition.EqualTo => RuleValueHelper.AreEqual(fieldValue, ruleValue),
            RuleCondition.NotEqualTo => !RuleValueHelper.AreEqual(fieldValue, ruleValue),
            RuleCondition.GreaterThan => RuleValueHelper.Compare(fieldValue, ruleValue) > 0,
            RuleCondition.LessThan => RuleValueHelper.Compare(fieldValue, ruleValue) < 0,
            RuleCondition.GreaterThanEqual => RuleValueHelper.Compare(fieldValue, ruleValue) >= 0,
            RuleCondition.LessThanEqual => RuleValueHelper.Compare(fieldValue, ruleValue) <= 0,
            RuleCondition.In when ruleValue is IEnumerable<object?> inValues =>
                inValues.Any(x => RuleValueHelper.AreEqual(fieldValue, RuleValueHelper.Extract(x))),
            RuleCondition.NotIn when ruleValue is IEnumerable<object?> notInValues =>
                !notInValues.Any(x => RuleValueHelper.AreEqual(fieldValue, RuleValueHelper.Extract(x))),
            RuleCondition.Between when ruleValue is IEnumerable<object?> bounds => IsBetween(fieldValue, bounds),
            RuleCondition.Contains when fieldValue is string stringValue && ruleValue is string comparand =>
                stringValue.Contains(comparand, StringComparison.OrdinalIgnoreCase),
            RuleCondition.StartsWith when fieldValue is string stringValue && ruleValue is string comparand =>
                stringValue.StartsWith(comparand, StringComparison.OrdinalIgnoreCase),
            RuleCondition.EndsWith when fieldValue is string stringValue && ruleValue is string comparand =>
                stringValue.EndsWith(comparand, StringComparison.OrdinalIgnoreCase),
            RuleCondition.Matches when fieldValue is string stringValue && ruleValue is string pattern =>
                IsMatch(stringValue, pattern),
            _ => false
        };
    }

    private static bool IsBetween(object fieldValue, IEnumerable<object?> bounds)
    {
        var pair = bounds.Take(3).Select(RuleValueHelper.Extract).ToList();

        if (pair.Count != 2)
        {
            return false;
        }

        return RuleValueHelper.Compare(fieldValue, pair[0]) >= 0 && RuleValueHelper.Compare(fieldValue, pair[1]) <= 0;
    }

    private static bool IsMatch(string value, string pattern)
    {
        if (pattern.Length > MaxPatternLength)
        {
            return false;
        }

        return Compile(pattern)?.IsMatch(value) == true;
    }

    private static void Collect(RuleBodyModel rule, JsonObject payload, bool negated, DateTimeOffset now, IReadOnlyList<PartyFacts>? parties, VelocityFacts? velocity, List<ConditionMatch> conditionMatches)
    {
        // Mirrors Evaluate: when both And and Or are present only And runs.
        if (rule.Not is { Length: > 0 })
        {
            Collect(rule.Not.First(), payload, !negated, now, parties, velocity, conditionMatches);

            return;
        }

        if (rule.Field != null || rule.Expression != null || rule.Party != null || rule.Velocity != null)
        {
            conditionMatches.Add(new ConditionMatch
            {
                Field = DescribeLeaf(rule),
                Condition = rule.Condition,
                Quantifier = rule.Quantifier,
                Expected = ToJsonNode(rule.Value),
                Actual = ActualOf(rule, payload, now, parties, velocity),
                Matched = Evaluate(rule, payload, now, parties, velocity) != negated
            });

            return;
        }

        if (rule.And != null)
        {
            foreach (var child in rule.And)
            {
                Collect(child, payload, negated, now, parties, velocity, conditionMatches);
            }

            return;
        }

        if (rule.Or != null)
        {
            foreach (var child in rule.Or)
            {
                Collect(child, payload, negated, now, parties, velocity, conditionMatches);
            }
        }
    }

    private static string DescribeLeaf(RuleBodyModel rule)
    {
        if (rule.Expression != null)
        {
            return RuleExpressionEvaluator.Describe(rule.Expression);
        }

        if (rule.Party != null)
        {
            return PartyFactsReader.Describe(rule.Party);
        }

        return rule.Velocity != null ? VelocityFactsReader.Describe(rule.Velocity) : rule.Field!;
    }

    private static JsonNode? ActualOf(RuleBodyModel rule, JsonObject payload, DateTimeOffset now, IReadOnlyList<PartyFacts>? parties, VelocityFacts? velocity)
    {
        if (rule.Expression != null)
        {
            return ToJsonNode(RuleExpressionEvaluator.Evaluate(rule.Expression, payload, now, parties, velocity));
        }

        if (rule.Party != null)
        {
            return rule.Quantifier == null
                ? ToJsonNode(PartyFactsReader.ReadSingle(rule.Party, parties, now))
                : new JsonArray(PartyFactsReader.ReadAll(rule.Party, parties, now).Select(ToJsonNode).ToArray());
        }

        if (rule.Velocity != null)
        {
            return rule.Quantifier == null
                ? ToJsonNode(VelocityFactsReader.ReadSingle(rule.Velocity, parties, velocity, now))
                : new JsonArray(VelocityFactsReader.ReadAll(rule.Velocity, parties, velocity, now).Select(ToJsonNode).ToArray());
        }

        if (rule.Quantifier == null)
        {
            return PayloadPath.ResolveSingle(payload, rule.Field!)?.DeepClone();
        }

        return new JsonArray(PayloadPath.ResolveAll(payload, rule.Field!).Select(x => x?.DeepClone()).ToArray());
    }

    private static JsonNode? ToJsonNode(object? value)
    {
        return value is null ? null : JsonSerializer.SerializeToNode(value);
    }
}
