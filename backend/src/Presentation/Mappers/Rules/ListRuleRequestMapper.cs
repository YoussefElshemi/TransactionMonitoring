using Core.Models.Rules;
using Core.ValueObjects;
using Presentation.Models.Rules;

namespace Presentation.Mappers.Rules;

public static class ListRuleRequestMapper
{
    public static ListRuleRequest ToModel(this ListRuleRequestDto listRuleRequestDto)
    {
        return new ListRuleRequest
        {
            OrganizationId = new OrganizationId(listRuleRequestDto.OrganizationId),
            RuleSetId = new RuleSetId(listRuleRequestDto.RuleSetId),
            FriendlyName = !string.IsNullOrWhiteSpace(listRuleRequestDto.FriendlyName) ? new FriendlyName(listRuleRequestDto.FriendlyName) : null,
            PageNumber = new PageNumber(listRuleRequestDto.PageNumber),
            PageSize = new PageSize(listRuleRequestDto.PageSize),
            FromDate = new DateRange(listRuleRequestDto.FromDate),
            ToDate = new DateRange(listRuleRequestDto.ToDate)
        };
    }
}