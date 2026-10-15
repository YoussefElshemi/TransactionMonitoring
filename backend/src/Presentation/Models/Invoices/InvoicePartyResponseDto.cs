namespace Presentation.Models.Invoices;

public record InvoicePartyResponseDto
{
    public required string LegalName { get; init; }
    public required string Address { get; init; }
    public required string CountryCode { get; init; }
    public required string? TaxIdentifier { get; init; }
    public required string? Email { get; init; }
}
