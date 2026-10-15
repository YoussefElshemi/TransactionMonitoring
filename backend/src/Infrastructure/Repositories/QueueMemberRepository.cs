using Core.Extensions;
using Core.Interfaces.Repositories;
using Core.Models.Queues;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class QueueMemberRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : IQueueMemberRepository
{
    public async Task<List<QueueMember>> ListAsync(QueueId queueId, CancellationToken stoppingToken)
    {
        var queueMemberEntities = await context.QueueMembers
            .Include(x => x.User)
            .ThenInclude(x => x.Organization)
            .Where(x => x.QueueId == queueId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(stoppingToken);

        return queueMemberEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<Exists> ExistsAsync(QueueId queueId, ApplicationUserId userId, CancellationToken stoppingToken)
    {
        var exists = await context.QueueMembers
            .AnyAsync(x => x.QueueId == queueId && x.UserId == userId, stoppingToken);

        return new Exists(exists);
    }

    public async Task<List<QueueMemberLoad>> GetLoadsAsync(QueueId queueId, CancellationToken stoppingToken)
    {
        var queueMemberEntities = await context.QueueMembers
            .Include(x => x.User)
            .ThenInclude(x => x.Organization)
            .Where(x => x.QueueId == queueId)
            .ToListAsync(stoppingToken);

        if (queueMemberEntities.Count == 0)
        {
            return [];
        }

        var userIds = queueMemberEntities.ConvertAll(x => x.UserId);
        var organizationId = queueMemberEntities[0].User.OrganizationId;

        var counts = await context.Cases
            .Where(x => x.OrganizationId == organizationId &&
                        x.AssignedToUserId.HasValue &&
                        userIds.Contains(x.AssignedToUserId.Value) &&
                        !x.IsDeleted &&
                        !CaseStatusExtensions.TerminalStatusIds.Contains(x.StatusId))
            .GroupBy(x => x.AssignedToUserId!.Value)
            .Select(x => new { UserId = x.Key, Count = x.Count() })
            .ToListAsync(stoppingToken);

        var countsByUser = counts.ToDictionary(x => x.UserId, x => x.Count);

        return queueMemberEntities.ConvertAll(x => new QueueMemberLoad
        {
            UserId = new ApplicationUserId(x.UserId),
            User = x.User is not null ? x.User.ToModel() : null,
            OpenCaseCount = new OpenCaseCount(countsByUser.GetValueOrDefault(x.UserId, 0)),
            LastAssignedAt = x.LastAssignedAt.HasValue ? new LastAssignedAt(x.LastAssignedAt.Value) : null
        });
    }

    public async Task SetAsync(SetQueueMembersRequest setQueueMembersRequest, CancellationToken stoppingToken)
    {
        var existing = await context.QueueMembers
            .Where(x => x.QueueId == setQueueMembersRequest.QueueId)
            .ToListAsync(stoppingToken);

        var userIds = setQueueMembersRequest.UserIds.ConvertAll(x => (Guid)x);
        var departing = existing.Where(x => !userIds.Contains(x.UserId)).ToList();

        context.QueueMembers.RemoveRange(departing);

        var joining = userIds
            .Where(userId => !existing.Exists(x => x.UserId == userId))
            .Select(userId => new QueueMemberEntity
            {
                QueueId = setQueueMembersRequest.QueueId,
                UserId = userId,
                LastAssignedAt = null,
                CreatedAt = timeProvider.GetUtcNow(),
                UpdatedAt = timeProvider.GetUtcNow()
            })
            .ToList();

        await context.QueueMembers.AddRangeAsync(joining, stoppingToken);
    }

    public async Task<Count> ReplaceMemberAsync(
        OrganizationId organizationId, ApplicationUserId fromUserId, ApplicationUserId? toUserId, CancellationToken stoppingToken)
    {
        Guid fromUserIdValue = fromUserId;

        var memberships = await context.QueueMembers
            .Where(x => x.UserId == fromUserIdValue && x.Queue.OrganizationId == organizationId)
            .ToListAsync(stoppingToken);

        if (memberships.Count == 0)
        {
            return new Count(0);
        }

        if (toUserId is { } successorUserId)
        {
            Guid successorUserIdValue = successorUserId;
            var queueIds = memberships.ConvertAll(x => x.QueueId);

            var alreadyOn = await context.QueueMembers
                .Where(x => x.UserId == successorUserIdValue && queueIds.Contains(x.QueueId))
                .Select(x => x.QueueId)
                .ToListAsync(stoppingToken);

            var now = timeProvider.GetUtcNow();

            await context.QueueMembers.AddRangeAsync(
                queueIds
                    .Where(x => !alreadyOn.Contains(x))
                    .Select(x => new QueueMemberEntity
                    {
                        QueueId = x,
                        UserId = successorUserIdValue,
                        LastAssignedAt = null,
                        CreatedAt = now,
                        UpdatedAt = now
                    }),
                stoppingToken);
        }

        context.QueueMembers.RemoveRange(memberships);

        return new Count(memberships.Count);
    }

    public async Task TouchAsync(QueueId queueId, ApplicationUserId userId, CancellationToken stoppingToken)
    {
        var queueMemberEntity = await context.QueueMembers
            .FirstOrDefaultAsync(x => x.QueueId == queueId && x.UserId == userId, stoppingToken);

        if (queueMemberEntity is null)
        {
            return;
        }

        queueMemberEntity.LastAssignedAt = timeProvider.GetUtcNow();
        queueMemberEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.QueueMembers.Update(queueMemberEntity);
    }
}
