namespace Core.ValueObjects;

public readonly record struct SimulationId
{
    public SimulationId(Guid value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(SimulationId));
        Value = value;
    }

    private Guid Value { get; }

    public static implicit operator Guid(SimulationId valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
