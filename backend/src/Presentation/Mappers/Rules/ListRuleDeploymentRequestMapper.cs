using Core.Models.RuleDeployments;
using Core.ValueObjects;
using Presentation.Models.Rules;

namespace Presentation.Mappers.Rules;

public static class ListRuleDeploymentRequestMapper
{
    public static ListRuleDeploymentRequest ToModel(this ListRuleDeploymentRequestDto dto, RuleId ruleId)
    {
        return new ListRuleDeploymentRequest
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
