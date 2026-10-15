using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Billing;

public record UpdateBillingEntityRequest
{
    public required BillingEntityId BillingEntityId { get; init; }
    public required LegalName? LegalName { get; init; }
    public required PostalAddress? Address { get; init; }
    public required CountryCode? CountryCode { get; init; }
    public required TaxIdentifier? TaxIdentifier { get; init; }
    public required Currency? Currency { get; init; }
    public required IsEnabled? IsEnabled { get; init; }
}
