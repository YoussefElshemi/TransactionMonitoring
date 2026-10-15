using Core.Models.Common;
using Core.Models.Plans;
using Core.ValueObjects;
using Presentation.Models.Plans;

namespace Presentation.Mappers.Plans;

public static class UpdatePlanRequestMapper
{
    public static UpdatePlanRequest ToModel(this UpdatePlanRequestDto dto)
    {
        return new UpdatePlanRequest
        {
            PlanId = new PlanId(dto.PlanId),
            Name = !string.IsNullOrWhiteSpace(dto.Body.Name) ? new PlanName(dto.Body.Name) : null,
            Description = dto.Body.Description is null
                ? default
                : Patch<PlanDescription?>.Set(string.IsNullOrWhiteSpace(dto.Body.Description) ? null : new PlanDescription(dto.Body.Description)),
            IsEnabled = dto.Body.IsEnabled.HasValue ? new IsEnabled(dto.Body.IsEnabled.Value) : null
        };
    }
}
