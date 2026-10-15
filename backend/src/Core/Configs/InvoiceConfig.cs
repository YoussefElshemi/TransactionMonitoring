namespace Core.Configs;

public record InvoiceConfig
{
    public int DaysUntilDue { get; init; }

    public string CompanyName { get; init; } = null!;
}