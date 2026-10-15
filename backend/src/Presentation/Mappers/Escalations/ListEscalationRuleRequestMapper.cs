using Core.Models.Escalations;
using Core.ValueObjects;
using Presentation.Models.Escalations;

namespace Presentation.Mappers.Escalations;

public static class ListEscalationRuleRequestMapper
{
    public static ListEscalationRuleRequest ToModel(this ListEscalationRuleRequestDto listEscalationRuleRequestDto)
    {
        return new ListEscalationRuleRequest
        {
            OrganizationId = new OrganizationId(listEscalationRuleRequestDto.OrganizationId),
            Name = !string.IsNullOrWhiteSpace(listEscalationRuleRequestDto.Name)
                ? new EscalationRuleName(listEscalationRuleRequestDto.Name)
                : null,
            IsEnabled = listEscalationRuleRequestDto.IsEnabled.HasValue
                ? new IsEnabled(listEscalationRuleRequestDto.IsEnabled.Value)
                : null,
            PageNumber = new PageNumber(listEscalationRuleRequestDto.PageNumber),
            PageSize = new PageSize(listEscalationRuleRequestDto.PageSize),
            FromDate = new DateRange(listEscalationRuleRequestDto.FromDate),
            ToDate = new DateRange(listEscalationRuleRequestDto.ToDate)
        };
    }
}
