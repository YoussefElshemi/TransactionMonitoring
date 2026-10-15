using Core.Enums;
using Core.Models.CaseReasonCodes;
using Core.ValueObjects;
using Presentation.Models.CaseReasonCodes;

namespace Presentation.Mappers.CaseReasonCodes;

public static class ListCaseReasonCodeRequestMapper
{
    public static ListCaseReasonCodeRequest ToModel(this ListCaseReasonCodeRequestDto listCaseReasonCodeRequestDto)
    {
        return new ListCaseReasonCodeRequest
        {
            OrganizationId = new OrganizationId(listCaseReasonCodeRequestDto.OrganizationId),
            Code = !string.IsNullOrWhiteSpace(listCaseReasonCodeRequestDto.Code) ? new CaseReasonCodeName(listCaseReasonCodeRequestDto.Code) : null,
            Disposition = !string.IsNullOrWhiteSpace(listCaseReasonCodeRequestDto.Disposition)
                ? Enum.Parse<CaseDisposition>(listCaseReasonCodeRequestDto.Disposition, true)
                : null,
            IsEnabled = listCaseReasonCodeRequestDto.IsEnabled.HasValue ? new IsEnabled(listCaseReasonCodeRequestDto.IsEnabled.Value) : null,
            PageNumber = new PageNumber(listCaseReasonCodeRequestDto.PageNumber),
            PageSize = new PageSize(listCaseReasonCodeRequestDto.PageSize),
            FromDate = new DateRange(listCaseReasonCodeRequestDto.FromDate),
            ToDate = new DateRange(listCaseReasonCodeRequestDto.ToDate)
        };
    }
}
