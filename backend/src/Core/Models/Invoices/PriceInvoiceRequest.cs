using Core.Enums;
using Core.Localization;
using Core.Models.Adjustments;
using Core.Models.Commitments;
using Core.Models.Discounts;
using Core.Models.Plans;
using Core.Models.Tax;
using Core.ValueObjects;

namespace Core.Models.Invoices;

public record PriceInvoiceRequest
{
    public required InvoiceId InvoiceId { get; init; }
    public required PlanVersion PlanVersion { get; init; }

    public required Dictionary<BillableResource, Count> Quantities { get; init; }

    public required List<ContractDiscount> Discounts { get; init; }
    public required List<InvoiceAdjustment> Adjustments { get; init; }

    public required Commitment? Commitment { get; init; }

    public required Currency Currency { get; init; }
    public required ConversionRate ConversionRate { get; init; }

    public required TaxDetermination TaxDetermination { get; init; }

    public required Translator Translator { get; init; }
}
