using Core.Enums;
using Core.Models.Cases;
using Core.ValueObjects;
using Presentation.Models.Cases;

namespace Presentation.Mappers.Cases;

public static class ResolveCaseRequestMapper
{
    public static ResolveCaseRequest ToModel(this ResolveCaseRequestDto resolveCaseRequestDto)
    {
        return new ResolveCaseRequest
        {
            CaseId = new CaseId(resolveCaseRequestDto.CaseId),
            OrganizationId = new OrganizationId(resolveCaseRequestDto.OrganizationId),
            Status = Enum.Parse<CaseStatus>(resolveCaseRequestDto.Body.Status, true),
            Disposition = Enum.Parse<CaseDisposition>(resolveCaseRequestDto.Body.Disposition, true),
            CaseReasonCodeId = resolveCaseRequestDto.Body.ReasonCodeId.HasValue ? new CaseReasonCodeId(resolveCaseRequestDto.Body.ReasonCodeId.Value) : null,
            Note = !string.IsNullOrWhiteSpace(resolveCaseRequestDto.Body.Note) ? new Message(resolveCaseRequestDto.Body.Note) : null,
            ResolvedByUserId = new ApplicationUserId(resolveCaseRequestDto.Body.UserId)
        };
    }
}
