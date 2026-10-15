namespace Core.ValueObjects;

public readonly record struct VelocityBackfillId
{
    public VelocityBackfillId(Guid value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(VelocityBackfillId));
        Value = value;
    }

    private Guid Value { get; }

    public static implicit operator Guid(VelocityBackfillId valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
