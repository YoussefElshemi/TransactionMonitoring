using Core.Models.Queues;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class QueueMemberMapper
{
    public static QueueMember ToModel(this QueueMemberEntity queueMemberEntity)
    {
        return new QueueMember
        {
            QueueId = new QueueId(queueMemberEntity.QueueId),
            UserId = new ApplicationUserId(queueMemberEntity.UserId),
            User = queueMemberEntity.User is not null ? queueMemberEntity.User.ToModel() : null,
            LastAssignedAt = queueMemberEntity.LastAssignedAt.HasValue ? new LastAssignedAt(queueMemberEntity.LastAssignedAt.Value) : null,
            CreatedAt = new CreatedAt(queueMemberEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(queueMemberEntity.UpdatedAt)
        };
    }
}
