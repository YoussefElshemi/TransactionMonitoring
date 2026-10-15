using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Billing;
using Core.Models.Organizations;
using Core.Models.Plans;
using Core.ValueObjects;

namespace Core.Models.Invoices;

public record Invoice : ProcessableModel
{
    public required InvoiceId InvoiceId { get; init; }

    public required InvoiceNumber? InvoiceNumber { get; init; }

    public required InvoiceStatus InvoiceStatus { get; init; }

    public required BillingEntityId? BillingEntityId { get; init; }

    public required PlanVersionId? PlanVersionId { get; init; }
    public required PlanVersion? PlanVersion { get; init; }

    public required CostPerRequest CostPerRequest { get; init; }

    public required DocumentParty? Supplier { get; init; }
    public required DocumentParty? Customer { get; init; }

    public required Currency Currency { get; init; }
    public required Currency PlanCurrency { get; init; }
    public required ConversionRate? ConversionRate { get; init; }

    public required TaxTreatment TaxTreatment { get; init; }
    public required TaxRate TaxRate { get; init; }
    public required TaxAmount TaxAmount { get; init; }

    public required NetAmount NetAmount { get; init; }
    public required InvoiceAmount Amount { get; init; }

    public required Month Month { get; init; }
    public required Year Year { get; init; }

    public required IssuedAt? IssuedAt { get; init; }
    public required DueAt? DueAt { get; init; }
    public required PaymentTerms PaymentTerms { get; init; }

    public required OrganizationId OrganizationId { get; init; }
    public required Organization? Organization { get; init; }
    public required ApplicationUserId? CreatedByUserId { get; init; }

    public required List<InvoiceLine> Lines { get; init; }

    public required Locale? Locale { get; init; }
}
