namespace Presentation.Attributes;

// The architecture test accepts this in place of [Idempotent], so an exemption must say why.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IdempotencyExemptAttribute(string reason) : Attribute
{
    public string Reason { get; } = reason;
}
