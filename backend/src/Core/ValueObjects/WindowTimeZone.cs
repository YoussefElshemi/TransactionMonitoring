namespace Core.ValueObjects;

public readonly record struct WindowTimeZone
{
    public const int MaxLength = 64;

    public WindowTimeZone(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(WindowTimeZone));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(WindowTimeZone));

        if (!TryResolve(value, out _))
        {
            throw new ArgumentException($"'{value}' is not a time zone this host knows.", nameof(WindowTimeZone));
        }

        Value = value;
    }

    private string Value { get; }

    public TimeZoneInfo Resolve()
    {
        TryResolve(Value, out var timeZone);

        return timeZone;
    }

    public static bool IsKnown(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length <= MaxLength && TryResolve(value, out _);
    }

    public static implicit operator string(WindowTimeZone valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }

    private static bool TryResolve(string value, out TimeZoneInfo timeZone)
    {
        return TimeZoneInfo.TryFindSystemTimeZoneById(value, out timeZone!);
    }
}
