using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Billing;

public record CreateBillingEntityRequest
{
    public required LegalName LegalName { get; init; }
    public required PostalAddress Address { get; init; }
    public required CountryCode CountryCode { get; init; }
    public required TaxIdentifier? TaxIdentifier { get; init; }
    public required NumberPrefix InvoiceNumberPrefix { get; init; }
    public required NumberPrefix CreditNoteNumberPrefix { get; init; }
    public required Currency Currency { get; init; }
    public required ApplicationUserId? CreatedByUserId { get; init; }
}
