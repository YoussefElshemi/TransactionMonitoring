using Core.Enums;

namespace Core.Helpers;

// The braces are a Redis Cluster hash tag: every key one decision touches must share a slot for the atomic script.
public static class RateLimitKeyHelper
{
    public static string OrganizationTag(Guid organizationId)
    {
        return $"org:{organizationId}";
    }

    public static string ForOrganization(string prefix, Guid organizationId, Mode mode, RateLimitPool pool)
    {
        return $"{prefix}:{{{OrganizationTag(organizationId)}}}:{mode}:{pool}";
    }

    public static string ForCallerWithinOrganization(string prefix, Guid organizationId, string callerKey, Mode mode, RateLimitPool pool)
    {
        return $"{prefix}:{{{OrganizationTag(organizationId)}}}:{callerKey}:{mode}:{pool}";
    }

    public static string ForCaller(string prefix, string callerKey, Mode mode, RateLimitPool pool)
    {
        return $"{prefix}:{{{callerKey}}}:{mode}:{pool}";
    }

    public static string ForAnonymous(string prefix, string anonymousKey)
    {
        return $"{prefix}:{{{anonymousKey}}}:anon";
    }
}
