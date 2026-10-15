namespace Core.ValueObjects;

public readonly record struct MaintenanceWindowDescription
{
    public const int MaxLength = 2000;

    public MaintenanceWindowDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(MaintenanceWindowDescription));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(MaintenanceWindowDescription));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(MaintenanceWindowDescription valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
