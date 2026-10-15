namespace Core.ValueObjects;

public readonly record struct IncludeApplied
{
    public IncludeApplied(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IncludeApplied valueObject)
    {
        return valueObject.Value;
    }
}
