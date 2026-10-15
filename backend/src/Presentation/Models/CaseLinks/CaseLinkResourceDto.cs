using System.Text.Json.Serialization;
using Presentation.Models.Cases;
using Presentation.Models.Users;

namespace Presentation.Models.CaseLinks;

public record CaseLinkResourceDto
{
    public required CaseLinkResponseDto CaseLink { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CaseResponseDto? LinkedCase { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? CreatedByUser { get; init; }
}
