using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Tax;

public record TaxDetermination
{
    public required TaxTreatment TaxTreatment { get; init; }
    public required TaxRate TaxRate { get; init; }

    public required InvoiceLineDescription Description { get; init; }
}
