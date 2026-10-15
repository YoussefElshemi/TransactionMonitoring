using System.Text.Json.Serialization;
using Presentation.Models.Organizations;
using Presentation.Models.Payments;
using Presentation.Models.Users;

namespace Presentation.Models.Invoices;

public record InvoiceResourceDto
{
    public required InvoiceResponseDto Invoice { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? CreatedByUser { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InvoiceSettlementResponseDto? Settlement { get; init; }
}
