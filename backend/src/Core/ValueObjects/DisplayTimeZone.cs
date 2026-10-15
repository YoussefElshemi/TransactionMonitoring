namespace Core.ValueObjects;

public readonly record struct DisplayTimeZone
{
    public const int MaxLength = 64;

    public DisplayTimeZone(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(DisplayTimeZone));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(DisplayTimeZone));

        if (!TryResolve(value, out var timeZone))
        {
            throw new ArgumentException($"'{value}' is not a time zone this host knows.", nameof(DisplayTimeZone));
        }

        // Normalised to the IANA id: Windows and IANA spellings are both accepted on input.
        Value = Iana(timeZone);
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

    public static implicit operator string(DisplayTimeZone valueObject)
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

    // Windows ids with no IANA equivalent keep their own.
    public static string Iana(TimeZoneInfo timeZone)
    {
        if (timeZone.HasIanaId)
        {
            return timeZone.Id;
        }

        return TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZone.Id, out var ianaId) ? ianaId : timeZone.Id;
    }
}
