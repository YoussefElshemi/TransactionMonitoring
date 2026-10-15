namespace Core.ValueObjects;

public readonly record struct IsBreached
{
    public IsBreached(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IsBreached valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
