namespace Core.ValueObjects;

public readonly record struct VelocityObservationId
{
    public VelocityObservationId(Guid value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(VelocityObservationId));
        Value = value;
    }

    private Guid Value { get; }

    public static implicit operator Guid(VelocityObservationId valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
