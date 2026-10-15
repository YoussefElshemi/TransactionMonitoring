namespace Core.ValueObjects;

public readonly record struct AuditAnchorId
{
    public AuditAnchorId(Guid value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(AuditAnchorId));
        Value = value;
    }

    private Guid Value { get; }

    public static implicit operator Guid(AuditAnchorId valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
