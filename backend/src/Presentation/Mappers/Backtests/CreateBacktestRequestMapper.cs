using Core.Models.BackTests;
using Core.ValueObjects;
using Presentation.Models.BackTests;

namespace Presentation.Mappers.BackTests;

public static class CreateBackTestRequestMapper
{
    public static CreateBackTestRequest ToModel(this CreateBackTestRequestDto dto)
    {
        return new CreateBackTestRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            RuleId = new RuleId(dto.RuleId),
            VersionNumber = dto.Body?.VersionNumber is { } versionNumber ? new VersionNumber(versionNumber) : null,
            SampleSize = new SampleSize(dto.Body?.SampleSize ?? 100),
            CreatedByUserId = new ApplicationUserId(dto.CreatedByUserId)
        };
    }
}
