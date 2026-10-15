using Core.Enums;
using Core.Models.PartyMappings;
using Core.Models.Rules;
using Core.Models.TransactionSchemas;

namespace Core.Helpers;

public static class PartyMappingHelper
{
    private static readonly RuleCondition[] SubstringConditions =
    [
        RuleCondition.Contains,
        RuleCondition.StartsWith,
        RuleCondition.EndsWith,
        RuleCondition.Matches
    ];

    public static List<SchemaViolation> ValidateRuleBody(RuleBodyModel ruleBody, PartyMapping? partyMapping)
    {
        var violations = new List<SchemaViolation>();

        Collect(ruleBody, partyMapping, violations);

        return violations;
    }

    public static List<PartyRole> CollectRoles(RuleBodyModel ruleBody)
    {
        var roles = new List<PartyRole>();

        CollectRoles(ruleBody, roles);

        return roles.Distinct().ToList();
    }

    private static void CollectRoles(RuleBodyModel rule, List<PartyRole> roles)
    {
        if (rule.Party != null)
        {
            roles.Add(rule.Party.Role);
        }

        if (rule.Velocity != null)
        {
            roles.Add(rule.Velocity.Role);
        }

        if (rule.Expression != null)
        {
            CollectRoles(rule.Expression, roles);
        }

        foreach (var child in (rule.And ?? []).Concat(rule.Or ?? []).Concat(rule.Not ?? []))
        {
            CollectRoles(child, roles);
        }
    }

    private static void CollectRoles(RuleExpressionModel expression, List<PartyRole> roles)
    {
        if (expression.Party != null)
        {
            roles.Add(expression.Party.Role);
        }

        if (expression.Velocity != null)
        {
            roles.Add(expression.Velocity.Role);
        }

        foreach (var argument in expression.Arguments ?? [])
        {
            CollectRoles(argument, roles);
        }
    }

    private static void Collect(RuleBodyModel rule, PartyMapping? partyMapping, List<SchemaViolation> violations)
    {
        if (rule.Party != null)
        {
            CollectLeafViolations(rule, rule.Party, PartyFactsReader.Describe(rule.Party), partyMapping, violations);
        }

        if (rule.Velocity != null)
        {
            CollectLeafViolations(rule, Read(rule.Velocity), VelocityFactsReader.Describe(rule.Velocity), partyMapping, violations);
        }

        if (rule.Expression != null)
        {
            CollectExpressionViolations(rule.Expression, partyMapping, violations);
        }

        foreach (var child in (rule.And ?? []).Concat(rule.Or ?? []).Concat(rule.Not ?? []))
        {
            Collect(child, partyMapping, violations);
        }
    }

    private static void CollectExpressionViolations(RuleExpressionModel expression, PartyMapping? partyMapping, List<SchemaViolation> violations)
    {
        if (expression.Party != null && !TryResolveRole(expression.Party, partyMapping, out _, out var failure))
        {
            violations.Add(Violation(PartyFactsReader.Describe(expression.Party), failure));
        }

        if (expression.Velocity != null && !TryResolveRole(Read(expression.Velocity), partyMapping, out _, out var velocityFailure))
        {
            violations.Add(Violation(VelocityFactsReader.Describe(expression.Velocity), velocityFailure));
        }

        foreach (var argument in expression.Arguments ?? [])
        {
            CollectExpressionViolations(argument, partyMapping, violations);
        }
    }

    private static void CollectLeafViolations(RuleBodyModel rule, PartyConditionModel party, string label, PartyMapping? partyMapping, List<SchemaViolation> violations)
    {
        if (!TryResolveRole(party, partyMapping, out var resolvesToMany, out var failure))
        {
            violations.Add(Violation(label, failure));

            return;
        }

        var quantified = rule.Quantifier != null;

        if (resolvesToMany && !quantified)
        {
            violations.Add(Violation(label, "resolves to many parties, so the rule must state a Quantifier of 'Any' or 'All'"));
        }

        if (!resolvesToMany && quantified)
        {
            violations.Add(Violation(label, "resolves to a single party, so a Quantifier means nothing on it"));
        }

        if (rule.Condition is { } condition && SubstringConditions.Contains(condition))
        {
            violations.Add(Violation(label, $"is a number, so '{condition}' cannot be applied to it"));
        }
    }

    private static PartyConditionModel Read(VelocityConditionModel velocity)
    {
        return new PartyConditionModel { Role = velocity.Role, Attribute = PartyAttribute.RiskScore };
    }

    private static bool TryResolveRole(PartyConditionModel party, PartyMapping? partyMapping, out bool resolvesToMany, out string failure)
    {
        resolvesToMany = false;

        if (partyMapping == null)
        {
            failure = "cannot be read, because the organisation has no party mapping and so resolves nobody";

            return false;
        }

        var roleMapping = partyMapping.Mappings.FirstOrDefault(x => x.Role == party.Role);

        if (roleMapping == null)
        {
            failure = "is not a role the organisation's party mapping declares";

            return false;
        }

        failure = string.Empty;
        resolvesToMany = roleMapping.Path.HasValue && PayloadPath.HasWildcard(roleMapping.Path.Value);

        return true;
    }

    private static SchemaViolation Violation(string field, string message)
    {
        return new SchemaViolation
        {
            Field = field,
            Message = message
        };
    }
}
