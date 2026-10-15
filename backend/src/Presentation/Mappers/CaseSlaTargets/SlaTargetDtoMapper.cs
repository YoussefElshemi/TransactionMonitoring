using Core.Enums;
using Core.ValueObjects;
using Presentation.Models.CaseSlaTargets;

namespace Presentation.Mappers.CaseSlaTargets;

public static class SlaTargetDtoMapper
{
    public static SlaTargetResponseDto[] ToDto(this IReadOnlyDictionary<RuleSeverity, SlaTarget> slaTargets)
    {
        return slaTargets
            .OrderBy(x => (int)x.Key)
            .Select(x => new SlaTargetResponseDto
            {
                Severity = x.Key.ToString(),
                TargetMinutes = x.Value
            })
            .ToArray();
    }
}
