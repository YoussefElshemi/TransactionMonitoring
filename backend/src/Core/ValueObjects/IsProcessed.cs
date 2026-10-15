namespace Core.ValueObjects;

public readonly record struct IsProcessed
{
    public IsProcessed(bool value)
    {
        Value = value;
    }

    private bool Value { get; }

    public static implicit operator string(IsProcessed value)
    {
        return value.Value.ToString();
    }

    public static implicit operator bool(IsProcessed valueObject)
    {
        return valueObject.Value;
    }
}