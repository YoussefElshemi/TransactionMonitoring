namespace Core.ValueObjects;

public readonly record struct MaintenanceWindowTitle
{
    public const int MaxLength = 200;

    public MaintenanceWindowTitle(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(MaintenanceWindowTitle));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(MaintenanceWindowTitle));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(MaintenanceWindowTitle valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
