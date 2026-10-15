using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record BillingProfileEntity : DeletableEntity, IAuditable
{
    public required Guid BillingProfileId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid BillingEntityId { get; set; }
    public BillingEntityEntity BillingEntity { get; init; } = null!;
    public required string LegalName { get; set; }
    public required string ContactName { get; set; }
    public required string ContactEmail { get; set; }
    public required string Address { get; set; }
    public required string CountryCode { get; set; }
    public required string? TaxIdentifier { get; set; }
    public required DateTimeOffset? TaxIdentifierVerifiedAt { get; set; }
    public required string? ProviderCustomerReference { get; set; }

    public required int CurrencyId { get; set; }
    public CurrencyEntity Currency { get; init; } = null!;
    public required int PaymentTerms { get; set; }
    public required Guid? UpdatedByUserId { get; set; }
    public ApplicationUserEntity? UpdatedByUser { get; init; }
    public required string? Locale { get; set; }

    [NotMapped]
    public Guid AuditEntityId => BillingProfileId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.BillingProfile;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.BillingProfile;

    // Identity equality: the 1:1 Organization/BillingProfile pair makes record structural equality recurse forever.
    public virtual bool Equals(BillingProfileEntity? other)
    {
        return other is not null && BillingProfileId == other.BillingProfileId;
    }

    public override int GetHashCode()
    {
        return BillingProfileId.GetHashCode();
    }
}
