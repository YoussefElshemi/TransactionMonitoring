using Core.Models.Invoices;

namespace Core.Models.Payments;

public record ExportedReceipt
{
    public required Payment Payment { get; init; }

    public required Invoice? Invoice { get; init; }

    public required byte[] Content { get; init; }
}
