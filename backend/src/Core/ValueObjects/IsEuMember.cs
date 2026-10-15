namespace Core.ValueObjects;

public readonly record struct IsEuMember
{
    public IsEuMember(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator bool(IsEuMember valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
