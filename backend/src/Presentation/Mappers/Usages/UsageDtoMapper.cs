using Core.Models.Usages;
using Presentation.Models.Usages;

namespace Presentation.Mappers.Usages;

public static class UsageDtoMapper
{
    public static UsageResponseDto ToDto(this Usage usage)
    {
        return new UsageResponseDto
        {
            Mode = usage.Mode.ToString(),
            UsageId = usage.UsageId,
            Resource = usage.Resource,
            BillableResource = usage.BillableResource.ToString(),
            Count = usage.Count,
            CreatedAt = usage.CreatedAt
        };
    }
}