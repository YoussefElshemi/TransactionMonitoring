using Core.Enums;
using Core.Models.CaseReasonCodes;
using Core.ValueObjects;
using Presentation.Models.CaseReasonCodes;

namespace Presentation.Mappers.CaseReasonCodes;

public static class UpdateCaseReasonCodeRequestMapper
{
    public static UpdateCaseReasonCodeRequest ToModel(this UpdateCaseReasonCodeRequestDto updateCaseReasonCodeRequestDto)
    {
        return new UpdateCaseReasonCodeRequest
        {
            CaseReasonCodeId = new CaseReasonCodeId(updateCaseReasonCodeRequestDto.CaseReasonCodeId),
            Description = !string.IsNullOrWhiteSpace(updateCaseReasonCodeRequestDto.Body.Description)
                ? new CaseReasonCodeDescription(updateCaseReasonCodeRequestDto.Body.Description)
                : null,
            Disposition = !string.IsNullOrWhiteSpace(updateCaseReasonCodeRequestDto.Body.Disposition)
                ? Enum.Parse<CaseDisposition>(updateCaseReasonCodeRequestDto.Body.Disposition, true)
                : null,
            ClearDisposition = new ClearDisposition(updateCaseReasonCodeRequestDto.Body.ClearDisposition ?? false),
            IsEnabled = updateCaseReasonCodeRequestDto.Body.IsEnabled.HasValue
                ? new IsEnabled(updateCaseReasonCodeRequestDto.Body.IsEnabled.Value)
                : null
        };
    }
}
