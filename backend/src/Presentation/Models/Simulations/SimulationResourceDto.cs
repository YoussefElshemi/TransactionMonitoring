using System.Text.Json.Serialization;
using Presentation.Models.Organizations;
using Presentation.Models.Users;

namespace Presentation.Models.Simulations;

public record SimulationResourceDto
{
    public required SimulationResponseDto Simulation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrganizationResponseDto? Organization { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? CreatedByUser { get; init; }
}
