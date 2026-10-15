using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Invitations;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class InvitationRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListInvitationRequest, InvitationEntity, Invitation>, IInvitationRepository
{
    public async Task AddAsync(Invitation invitation, Hash tokenHash, CancellationToken stoppingToken)
    {
        await context.Invitations.AddAsync(invitation.ToEntity(tokenHash), stoppingToken);
    }

    public async Task<Invitation> GetAsync(InvitationId invitationId, CancellationToken stoppingToken)
    {
        var invitationEntity = await context.Invitations
            .FirstAsync(x => x.InvitationId == invitationId, stoppingToken);

        return invitationEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, InvitationId invitationId, CancellationToken stoppingToken)
    {
        var exists = await context.Invitations
            .AnyAsync(x => x.InvitationId == invitationId && x.OrganizationId == organizationId, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Invitation?> FindByTokenAsync(Hash tokenHash, CancellationToken stoppingToken)
    {
        var invitationEntity = await context.Invitations
            .FirstOrDefaultAsync(x => x.TokenHash == (string)tokenHash, stoppingToken);

        return invitationEntity?.ToModel();
    }

    public async Task<Invitation?> FindPendingByEmailAsync(OrganizationId organizationId, Email email, CancellationToken stoppingToken)
    {
        var normalizedEmail = ((string)email).Trim().ToUpperInvariant();
        var now = timeProvider.GetUtcNow();

        var invitationEntity = await context.Invitations
            .Where(x => x.OrganizationId == organizationId &&
                        x.NormalizedEmail == normalizedEmail &&
                        x.AcceptedAt == null &&
                        x.RevokedAt == null &&
                        x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(stoppingToken);

        return invitationEntity?.ToModel();
    }

    public async Task ReissueAsync(InvitationId invitationId, Hash tokenHash, ExpiresAt expiresAt, CancellationToken stoppingToken)
    {
        var invitationEntity = await context.Invitations
            .FirstAsync(x => x.InvitationId == invitationId, stoppingToken);

        invitationEntity.TokenHash = tokenHash;
        invitationEntity.ExpiresAt = expiresAt;
        invitationEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Invitations.Update(invitationEntity);
    }

    public async Task AcceptAsync(InvitationId invitationId, ApplicationUserId acceptedByUserId, CancellationToken stoppingToken)
    {
        var invitationEntity = await context.Invitations
            .FirstAsync(x => x.InvitationId == invitationId, stoppingToken);

        var now = timeProvider.GetUtcNow();

        invitationEntity.AcceptedAt = now;
        invitationEntity.AcceptedByUserId = acceptedByUserId;
        invitationEntity.UpdatedAt = now;

        context.Invitations.Update(invitationEntity);
    }

    public async Task RevokeAsync(InvitationId invitationId, ApplicationUserId revokedByUserId, CancellationToken stoppingToken)
    {
        var invitationEntity = await context.Invitations
            .FirstAsync(x => x.InvitationId == invitationId, stoppingToken);

        var now = timeProvider.GetUtcNow();

        invitationEntity.RevokedAt = now;
        invitationEntity.RevokedByUserId = revokedByUserId;
        invitationEntity.UpdatedAt = now;

        context.Invitations.Update(invitationEntity);
    }

    public async Task<Count> RevokePendingByInviterAsync(
        OrganizationId organizationId, ApplicationUserId invitedByUserId, ApplicationUserId revokedByUserId, CancellationToken stoppingToken)
    {
        Guid invitedByUserIdValue = invitedByUserId;
        var now = timeProvider.GetUtcNow();

        var invitationEntities = await context.Invitations
            .Where(x => x.OrganizationId == organizationId &&
                        x.InvitedByUserId == invitedByUserIdValue &&
                        x.AcceptedAt == null &&
                        x.RevokedAt == null &&
                        x.ExpiresAt > now)
            .ToListAsync(stoppingToken);

        foreach (var invitationEntity in invitationEntities)
        {
            invitationEntity.RevokedAt = now;
            invitationEntity.RevokedByUserId = revokedByUserId;
            invitationEntity.UpdatedAt = now;

            context.Invitations.Update(invitationEntity);
        }

        return new Count(invitationEntities.Count);
    }

    protected override Task<List<Invitation>> MapPageAsync(List<InvitationEntity> items, ListInvitationRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<InvitationEntity> OrderByExpression()
    {
        return new OrderByExpression<InvitationEntity, DateTimeOffset>(x => x.CreatedAt);
    }

    protected override IQueryable<InvitationEntity> FilterByPagedRequest(ListInvitationRequest listInvitationRequest)
    {
        var queryable = context.Invitations
            .Where(x => x.OrganizationId == listInvitationRequest.OrganizationId);

        if (!listInvitationRequest.IncludeResolved)
        {
            var now = timeProvider.GetUtcNow();

            queryable = queryable.Where(x => x.AcceptedAt == null && x.RevokedAt == null && x.ExpiresAt > now);
        }

        if (listInvitationRequest.Email.HasValue)
        {
            queryable = queryable.Where(x => EF.Functions.ILike(x.Email, $"%{listInvitationRequest.Email}%"));
        }

        return queryable;
    }

    protected override IQueryable<InvitationEntity> FilterByDateRange(IQueryable<InvitationEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }
}
