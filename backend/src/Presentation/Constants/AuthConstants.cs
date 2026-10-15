namespace Presentation.Constants;

public static class AuthConstants
{
    public const string ApiClientScheme = "ApiClient";

    public const string DefaultPolicy = "DefaultPolicy";
    public const string DefaultScheme = "DefaultScheme";

    public const string CookieName = "Auth";

    public const string ClientIdClaim = "client_id";
    public const string OrganizationIdClaim = "organization_id";
    public const string ScopeClaim = "scope";
    public const string SecretIdClaim = "secret_id";
    public const string ApiVersionClaim = "api_version";

    public static class ApiScopePolicies
    {
        public const string RulesEvaluate = "scope:rules:evaluate";
        public const string RuleSetsEvaluate = "scope:rulesets:evaluate";
    }
}
