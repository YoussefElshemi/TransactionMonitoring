using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Billing;

public record BillingProfile : DeletableModel
{
    public required BillingProfileId BillingProfileId { get; init; }
    public required OrganizationId OrganizationId { get; init; }

    public required BillingEntityId BillingEntityId { get; init; }
    public required BillingEntity? BillingEntity { get; init; }

    public required LegalName LegalName { get; init; }
    public required ContactName ContactName { get; init; }
    public required Email ContactEmail { get; init; }
    public required PostalAddress Address { get; init; }
    public required CountryCode CountryCode { get; init; }
    public required TaxIdentifier? TaxIdentifier { get; init; }

    public required VerifiedAt? TaxIdentifierVerifiedAt { get; init; }

    public required ProviderCustomerReference? ProviderCustomerReference { get; init; }

    public required Currency Currency { get; init; }
    public required PaymentTerms PaymentTerms { get; init; }
    public required ApplicationUserId? UpdatedByUserId { get; init; }

    public required Locale? Locale { get; init; }
}
