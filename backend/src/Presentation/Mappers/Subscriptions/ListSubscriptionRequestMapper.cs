using Core.Models.Subscriptions;
using Core.ValueObjects;
using Presentation.Models.Subscriptions;

namespace Presentation.Mappers.Subscriptions;

public static class ListSubscriptionRequestMapper
{
    public static ListSubscriptionRequest ToModel(this ListSubscriptionRequestDto dto)
    {
        return new ListSubscriptionRequest
        {
            OrganizationId = new OrganizationId(dto.OrganizationId),
            IncludeSuperseded = new IncludeSuperseded(dto.IncludeSuperseded ?? false),
            PageNumber = new PageNumber(dto.PageNumber),
            PageSize = new PageSize(dto.PageSize),
            FromDate = new DateRange(dto.FromDate),
            ToDate = new DateRange(dto.ToDate)
        };
    }
}
