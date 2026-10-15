namespace Core.Models.Common;

// Distinguishes "left alone" from "explicitly cleared", which a plain nullable cannot.
public readonly record struct Patch<T>
{
    private Patch(T value)
    {
        Value = value;
        IsSet = true;
    }

    public T Value { get; }

    public bool IsSet { get; }

    public static Patch<T> Set(T value)
    {
        return new Patch<T>(value);
    }
}
