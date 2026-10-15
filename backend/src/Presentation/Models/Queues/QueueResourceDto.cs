using System.Text.Json.Serialization;
using Presentation.Models.Organizations;

namespace Presentation.Models.Queues;

public record QueueResourceDto
{
    public required QueueResponseDto Queue { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }
}
