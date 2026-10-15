using Core.Enums;
using Core.Models.CaseReasonCodes;
using Core.ValueObjects;
using Presentation.Models.CaseReasonCodes;

namespace Presentation.Mappers.CaseReasonCodes;

public static class CreateCaseReasonCodeRequestMapper
{
    public static CreateCaseReasonCodeRequest ToModel(this CreateCaseReasonCodeRequestDto createCaseReasonCodeRequestDto)
    {
        return new CreateCaseReasonCodeRequest
        {
            OrganizationId = new OrganizationId(createCaseReasonCodeRequestDto.OrganizationId),
            Code = new CaseReasonCodeName(createCaseReasonCodeRequestDto.Body.Code),
            Description = new CaseReasonCodeDescription(createCaseReasonCodeRequestDto.Body.Description),
            Disposition = !string.IsNullOrWhiteSpace(createCaseReasonCodeRequestDto.Body.Disposition)
                ? Enum.Parse<CaseDisposition>(createCaseReasonCodeRequestDto.Body.Disposition, true)
                : null,
            IsEnabled = new IsEnabled(createCaseReasonCodeRequestDto.Body.IsEnabled)
        };
    }
}
