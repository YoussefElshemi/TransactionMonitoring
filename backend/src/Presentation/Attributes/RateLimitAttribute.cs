using Core.Enums;

namespace Presentation.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RateLimitAttribute(RateLimitPool pool) : Attribute
{
    public RateLimitPool Pool { get; } = pool;
}
