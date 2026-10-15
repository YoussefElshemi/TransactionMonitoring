using Core.Enums;

namespace Core.Helpers;

public static class ApiScopeHelper
{
    public const string PolicyPrefix = "scope:";

    private const string RulesEvaluate = "rules:evaluate";
    private const string RuleSetsEvaluate = "rulesets:evaluate";

    public static string ToScopeString(ApiScope apiScope)
    {
        return apiScope switch
        {
            ApiScope.RulesEvaluate => RulesEvaluate,
            ApiScope.RuleSetsEvaluate => RuleSetsEvaluate,
            _ => throw new ArgumentOutOfRangeException(nameof(apiScope), apiScope, null)
        };
    }

    public static string ToPolicyName(ApiScope apiScope)
    {
        return PolicyPrefix + ToScopeString(apiScope);
    }

    public static ApiScope Parse(string value)
    {
        if (!TryParse(value, out var apiScope))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown API scope.");
        }

        return apiScope;
    }

    public static bool TryParse(string value, out ApiScope apiScope)
    {
        switch (value)
        {
            case RulesEvaluate:
                apiScope = ApiScope.RulesEvaluate;
                return true;
            case RuleSetsEvaluate:
                apiScope = ApiScope.RuleSetsEvaluate;
                return true;
            default:
                apiScope = default;
                return false;
        }
    }
}
