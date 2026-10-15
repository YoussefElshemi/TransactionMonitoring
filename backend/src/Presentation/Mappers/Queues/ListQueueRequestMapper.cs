using Core.Enums;
using Core.Models.Queues;
using Core.ValueObjects;
using Presentation.Models.Queues;

namespace Presentation.Mappers.Queues;

public static class ListQueueRequestMapper
{
    public static ListQueueRequest ToModel(this ListQueueRequestDto listQueueRequestDto)
    {
        return new ListQueueRequest
        {
            OrganizationId = new OrganizationId(listQueueRequestDto.OrganizationId),
            Name = !string.IsNullOrWhiteSpace(listQueueRequestDto.Name) ? new QueueName(listQueueRequestDto.Name) : null,
            RoutingPolicy = !string.IsNullOrWhiteSpace(listQueueRequestDto.RoutingPolicy)
                ? Enum.Parse<QueueRoutingPolicy>(listQueueRequestDto.RoutingPolicy, true)
                : null,
            IsEnabled = listQueueRequestDto.IsEnabled.HasValue ? new IsEnabled(listQueueRequestDto.IsEnabled.Value) : null,
            MemberUserId = listQueueRequestDto.MemberUserId.HasValue
                ? new ApplicationUserId(listQueueRequestDto.MemberUserId.Value)
                : null,
            PageNumber = new PageNumber(listQueueRequestDto.PageNumber),
            PageSize = new PageSize(listQueueRequestDto.PageSize),
            FromDate = new DateRange(listQueueRequestDto.FromDate),
            ToDate = new DateRange(listQueueRequestDto.ToDate)
        };
    }
}
