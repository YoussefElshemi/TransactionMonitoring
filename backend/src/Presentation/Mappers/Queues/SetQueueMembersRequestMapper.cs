using Core.Models.Queues;
using Core.ValueObjects;
using Presentation.Models.Queues;

namespace Presentation.Mappers.Queues;

public static class SetQueueMembersRequestMapper
{
    public static SetQueueMembersRequest ToModel(this SetQueueMembersRequestDto setQueueMembersRequestDto)
    {
        return new SetQueueMembersRequest
        {
            QueueId = new QueueId(setQueueMembersRequestDto.QueueId),
            UserIds = setQueueMembersRequestDto.Body.UserIds.Select(x => new ApplicationUserId(x)).ToList()
        };
    }
}
