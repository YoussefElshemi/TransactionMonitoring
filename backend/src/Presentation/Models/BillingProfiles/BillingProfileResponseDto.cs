namespace Presentation.Models.BillingProfiles;

public record BillingProfileResponseDto
{
    public required Guid BillingProfileId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid BillingEntityId { get; init; }
    public required string? BillingEntityLegalName { get; init; }
    public required string LegalName { get; init; }
    public required string ContactName { get; init; }
    public required string ContactEmail { get; init; }
    public required string Address { get; init; }
    public required string CountryCode { get; init; }
    public required string? TaxIdentifier { get; init; }

    public required DateTimeOffset? TaxIdentifierVerifiedAt { get; init; }

    public required string Currency { get; init; }
    public required int PaymentTerms { get; init; }
    public required Guid? UpdatedByUserId { get; init; }

    public required string? Locale { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
