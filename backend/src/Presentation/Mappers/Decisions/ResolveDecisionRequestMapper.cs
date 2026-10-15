using Core.Enums;
using Core.Models.Decisions;
using Core.ValueObjects;
using Presentation.Models.Decisions;

namespace Presentation.Mappers.Decisions;

public static class ResolveDecisionRequestMapper
{
    public static ResolveDecisionByIdRequest ToModel(this ResolveDecisionRequestDto resolveDecisionRequestDto)
    {
        return new ResolveDecisionByIdRequest
        {
            DecisionId = new DecisionId(resolveDecisionRequestDto.DecisionId),
            Verdict = Enum.Parse<CaseStatus>(resolveDecisionRequestDto.Body.Status, true),
            Disposition = !string.IsNullOrWhiteSpace(resolveDecisionRequestDto.Body.Disposition)
                ? Enum.Parse<CaseDisposition>(resolveDecisionRequestDto.Body.Disposition, true)
                : null,
            CaseReasonCodeId = resolveDecisionRequestDto.Body.ReasonCodeId.HasValue ? new CaseReasonCodeId(resolveDecisionRequestDto.Body.ReasonCodeId.Value) : null,
            Note = !string.IsNullOrWhiteSpace(resolveDecisionRequestDto.Body.Note) ? new Message(resolveDecisionRequestDto.Body.Note) : null,
            ResolvedByUserId = new ApplicationUserId(resolveDecisionRequestDto.Body.UserId)
        };
    }
}
