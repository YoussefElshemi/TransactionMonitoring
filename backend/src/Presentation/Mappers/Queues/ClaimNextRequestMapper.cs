using Core.Models.Queues;
using Core.ValueObjects;
using Presentation.Models.Queues;

namespace Presentation.Mappers.Queues;

public static class ClaimNextRequestMapper
{
    public static ClaimNextRequest ToModel(this ClaimNextRequestDto claimNextRequestDto)
    {
        return new ClaimNextRequest
        {
            OrganizationId = new OrganizationId(claimNextRequestDto.OrganizationId),
            UserId = new ApplicationUserId(claimNextRequestDto.UserId),
            QueueId = claimNextRequestDto.QueueId.HasValue ? new QueueId(claimNextRequestDto.QueueId.Value) : null
        };
    }
}
