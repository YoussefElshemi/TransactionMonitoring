using System.Text.Json.Serialization;
using Presentation.Models.Organizations;

namespace Presentation.Models.TransactionSchemas;

public record TransactionSchemaResourceDto
{
    public required TransactionSchemaResponseDto TransactionSchema { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }
}
