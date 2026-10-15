using Core.Models.RuleSets;
using Core.ValueObjects;
using Presentation.Models.RuleSets;

namespace Presentation.Mappers.RuleSets;

public static class ListRuleSetRequestMapper
{
    public static ListRuleSetRequest ToModel(this ListRuleSetRequestDto listRuleSetRequestDto)
    {
        return new ListRuleSetRequest
        {
            OrganizationId = new OrganizationId(listRuleSetRequestDto.OrganizationId),
            FriendlyName = !string.IsNullOrWhiteSpace(listRuleSetRequestDto.FriendlyName) ? new FriendlyName(listRuleSetRequestDto.FriendlyName) : null,
            PageNumber = new PageNumber(listRuleSetRequestDto.PageNumber),
            PageSize = new PageSize(listRuleSetRequestDto.PageSize),
            FromDate = new DateRange(listRuleSetRequestDto.FromDate),
            ToDate = new DateRange(listRuleSetRequestDto.ToDate)
        };
    }
}