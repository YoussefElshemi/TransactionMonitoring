namespace Presentation.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RateLimitExemptAttribute : Attribute;
