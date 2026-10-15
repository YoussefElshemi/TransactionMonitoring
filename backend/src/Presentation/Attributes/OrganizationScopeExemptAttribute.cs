namespace Presentation.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class OrganizationScopeExemptAttribute(string reason) : Attribute
{
    public string Reason { get; } = reason;
}
