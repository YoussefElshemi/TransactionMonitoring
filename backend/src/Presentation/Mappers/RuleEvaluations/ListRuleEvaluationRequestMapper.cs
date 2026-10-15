using Core.Models.RuleEvaluations;
using Core.ValueObjects;
using Presentation.Models.RuleEvaluations;

namespace Presentation.Mappers.RuleEvaluations;

public static class ListRuleEvaluationRequestMapper
{
    public static ListRuleEvaluationRequest ToModel(this ListRuleEvaluationRequestDto dto)
    {
        return new ListRuleEvaluationRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            RuleId = dto.RuleId.HasValue ? new RuleId(dto.RuleId.Value) : null,
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }
}
