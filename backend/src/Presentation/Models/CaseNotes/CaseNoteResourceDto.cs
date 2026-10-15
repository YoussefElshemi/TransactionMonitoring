using System.Text.Json.Serialization;
using Presentation.Models.Users;

namespace Presentation.Models.CaseNotes;

public record CaseNoteResourceDto
{
    public required CaseNoteResponseDto CaseNote { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? CreatedByUser { get; init; }
}
