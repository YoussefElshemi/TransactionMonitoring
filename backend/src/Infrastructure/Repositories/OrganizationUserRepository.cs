using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Organizations;
using Core.Models.Users;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class OrganizationUserRepository(
    UserManager<ApplicationUserEntity> userManager) : PagedRepository<ListOrganizationUserRequest, ApplicationUserEntity, ApplicationUser>, IOrganizationUserRepository
{
    public async Task<Exists> ExistsByIdAsync(OrganizationId organizationId, ApplicationUserId userId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await userManager.Users
            .AnyAsync(x => x.Id == userId &&
                           x.OrganizationId == organizationId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    protected override Task<List<ApplicationUser>> MapPageAsync(List<ApplicationUserEntity> items, ListOrganizationUserRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<ApplicationUserEntity> OrderByExpression()
    {
        return new OrderByExpression<ApplicationUserEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<ApplicationUserEntity> FilterByPagedRequest(ListOrganizationUserRequest listOrganizationUserRequest)
    {
        var queryable = userManager.Users
            .Where(x => x.OrganizationId == listOrganizationUserRequest.OrganizationId && !x.IsDeleted);

        if (listOrganizationUserRequest.UserName.HasValue)
        {
            queryable = queryable
                .Where(x => x.UserName != null && EF.Functions.ILike(x.UserName, $"%{listOrganizationUserRequest.UserName}%"));
        }

        return queryable;
    }

    protected override IQueryable<ApplicationUserEntity> FilterByDateRange(IQueryable<ApplicationUserEntity> queryable, DatedRequest datedRequest)
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