namespace Core.Models.Invoices;

public record ExportedInvoice
{
    public required Invoice Invoice { get; init; }
    public required byte[] Content { get; init; }
}