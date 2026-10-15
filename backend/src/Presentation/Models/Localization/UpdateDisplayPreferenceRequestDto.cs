using System.Text.Json.Serialization;
using Core.Models.Common;

namespace Presentation.Models.Localization;

public record UpdateDisplayPreferenceRequestDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> Locale { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> TimeZone { get; init; }
}
