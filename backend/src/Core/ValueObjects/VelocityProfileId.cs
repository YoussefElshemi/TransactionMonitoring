namespace Core.ValueObjects;

public readonly record struct VelocityProfileId
{
    public VelocityProfileId(Guid value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(VelocityProfileId));
        Value = value;
    }

    private Guid Value { get; }

    public static implicit operator Guid(VelocityProfileId valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
