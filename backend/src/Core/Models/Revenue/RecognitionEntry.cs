using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Revenue;

public record RecognitionEntry
{
    public required RecognitionDocumentType DocumentType { get; init; }
    public required DocumentNumber DocumentNumber { get; init; }
    public required IssuedAt IssuedAt { get; init; }

    public required Month Month { get; init; }
    public required Year Year { get; init; }

    public required LegalName? BillingEntityName { get; init; }

    public required OrganizationId OrganizationId { get; init; }

    public required LegalName? CustomerName { get; init; }
    public required CountryCode? CustomerCountryCode { get; init; }

    public required Currency Currency { get; init; }
    public required NetAmount NetAmount { get; init; }
    public required TaxAmount TaxAmount { get; init; }
    public required InvoiceAmount Amount { get; init; }
    public required TaxTreatment TaxTreatment { get; init; }
    public required TaxRate TaxRate { get; init; }

    public required Currency ReportingCurrency { get; init; }
    public required ConversionRate ReportingRate { get; init; }
    public required RevenueAmount ReportingNetAmount { get; init; }
}
