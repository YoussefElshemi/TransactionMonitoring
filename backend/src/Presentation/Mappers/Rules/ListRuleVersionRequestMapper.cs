using Core.Models.Rules;
using Core.ValueObjects;
using Presentation.Models.Rules;

namespace Presentation.Mappers.Rules;

public static class ListRuleVersionRequestMapper
{
    public static ListRuleVersionRequest ToModel(this ListRuleVersionRequestDto dto, RuleId ruleId)
    {
        return new ListRuleVersionRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            RuleId = ruleId,
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }
}
