using System.Text.Json.Serialization;
using Presentation.Models.Organizations;

namespace Presentation.Models.TransactionSchemas;

public record TransactionSchemaVersionResourceDto
{
    public required TransactionSchemaVersionResponseDto TransactionSchemaVersion { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }
}
