using Core.Interfaces.Repositories;
using Core.Models.ApiClients;
using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ApiClientRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListApiClientRequest, ApiClientEntity, ApiClient>, IApiClientRepository
{
    public async Task AddAsync(ApiClient apiClient, CancellationToken stoppingToken)
    {
        var apiClientEntity = apiClient.ToEntity();
        await context.ApiClients.AddAsync(apiClientEntity, stoppingToken);
    }

    public async Task UpdateAsync(UpdateApiClientRequest updateApiClientRequest, CancellationToken stoppingToken)
    {
        var apiClientEntity = await context.ApiClients
            .IgnoreAutoIncludes()
            .FirstAsync(x => x.ApiClientId == updateApiClientRequest.ApiClientId && !x.IsDeleted, stoppingToken);

        apiClientEntity.FriendlyName = updateApiClientRequest.FriendlyName ?? apiClientEntity.FriendlyName;
        apiClientEntity.IsEnabled = updateApiClientRequest.IsEnabled ?? apiClientEntity.IsEnabled;
        apiClientEntity.ApiVersion = updateApiClientRequest.ApiVersion ?? apiClientEntity.ApiVersion;
        apiClientEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.ApiClients.Update(apiClientEntity);

        if (updateApiClientRequest.Scopes is null)
        {
            return;
        }

        Guid apiClientId = updateApiClientRequest.ApiClientId;
        var scopeIds = updateApiClientRequest.Scopes.Select(x => (int)x).ToHashSet();

        var existingScopes = await context.ApiClientScopes
            .AsTracking()
            .Where(x => x.ApiClientId == apiClientId)
            .ToListAsync(stoppingToken);

        context.ApiClientScopes.RemoveRange(existingScopes.Where(x => !scopeIds.Contains(x.ApiScopeId)));

        var existingScopeIds = existingScopes.Select(x => x.ApiScopeId).ToHashSet();

        await context.ApiClientScopes.AddRangeAsync(
            scopeIds
                .Where(x => !existingScopeIds.Contains(x))
                .Select(x => new ApiClientScopeEntity
                {
                    ApiClientId = apiClientId,
                    ApiScopeId = x,
                    CreatedAt = timeProvider.GetUtcNow(),
                    UpdatedAt = timeProvider.GetUtcNow()
                }),
            stoppingToken);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, ApiClientId apiClientId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.ApiClients
            .AnyAsync(x => x.ApiClientId == apiClientId &&
                           x.OrganizationId == organizationId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<Exists> IsActiveAsync(ApiClientId apiClientId, CancellationToken stoppingToken)
    {
        var exists = await context.ApiClients
            .AnyAsync(x => x.ApiClientId == apiClientId && x.IsEnabled && !x.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task<ApiClient> GetAsync(ApiClientId apiClientId, CancellationToken stoppingToken)
    {
        var apiClientEntity = await context.ApiClients
            .Include(x => x.Scopes)
            .FirstAsync(x => x.ApiClientId == apiClientId && !x.IsDeleted, stoppingToken);

        return apiClientEntity.ToModel();
    }

    public async Task<List<ApiClient>> ListByIdsAsync(IReadOnlyCollection<ApiClientId> apiClientIds, CancellationToken stoppingToken)
    {
        if (apiClientIds.Count == 0)
        {
            return [];
        }

        var apiClientIdValues = apiClientIds.Select(x => (Guid)x).Distinct().ToList();

        var apiClientEntities = await context.ApiClients
            .Include(x => x.Scopes)
            .Where(x => apiClientIdValues.Contains(x.ApiClientId))
            .ToListAsync(stoppingToken);

        return apiClientEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<ApiClient?> FindActiveByClientIdAsync(ClientId clientId, CancellationToken stoppingToken)
    {
        string value = clientId;

        var apiClientEntity = await context.ApiClients
            .Include(x => x.Scopes)
            .Include(x => x.Secrets.Where(secret => !secret.IsDeleted))
            .FirstOrDefaultAsync(x => x.ClientId == value && x.IsEnabled && !x.IsDeleted, stoppingToken);

        return apiClientEntity?.ToModel();
    }

    public async Task DeleteAsync(ApiClientId apiClientId, CancellationToken stoppingToken)
    {
        var apiClientEntity = await context.ApiClients
            .IgnoreAutoIncludes()
            .FirstAsync(x => x.ApiClientId == apiClientId, stoppingToken);

        apiClientEntity.IsDeleted = true;
        apiClientEntity.DeletedAt = timeProvider.GetUtcNow();

        context.ApiClients.Update(apiClientEntity);
    }

    public async Task RecoverAsync(ApiClientId apiClientId, CancellationToken stoppingToken)
    {
        var apiClientEntity = await context.ApiClients
            .IgnoreAutoIncludes()
            .FirstAsync(x => x.ApiClientId == apiClientId, stoppingToken);

        apiClientEntity.IsDeleted = false;
        apiClientEntity.DeletedAt = null;

        context.ApiClients.Update(apiClientEntity);
    }

    public async Task AddSecretAsync(ApiClientSecret apiClientSecret, CancellationToken stoppingToken)
    {
        var apiClientSecretEntity = apiClientSecret.ToEntity();
        await context.ApiClientSecrets.AddAsync(apiClientSecretEntity, stoppingToken);
    }

    public async Task<List<ApiClientSecret>> ListSecretsAsync(ApiClientId apiClientId, CancellationToken stoppingToken)
    {
        var apiClientSecretEntities = await context.ApiClientSecrets
            .Where(x => x.ApiClientId == apiClientId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(stoppingToken);

        return apiClientSecretEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<Exists> SecretExistsAsync(ApiClientId apiClientId, ApiClientSecretId apiClientSecretId, CancellationToken stoppingToken)
    {
        var exists = await context.ApiClientSecrets
            .AnyAsync(x => x.ApiClientSecretId == apiClientSecretId &&
                           x.ApiClientId == apiClientId &&
                           !x.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Exists> IsSecretActiveAsync(ApiClientSecretId apiClientSecretId, CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        var exists = await context.ApiClientSecrets
            .AnyAsync(x => x.ApiClientSecretId == apiClientSecretId &&
                           !x.IsDeleted &&
                           x.RevokedAt == null &&
                           (x.ExpiresAt == null || x.ExpiresAt > now), stoppingToken);

        return new Exists(exists);
    }

    public async Task RevokeSecretAsync(ApiClientSecretId apiClientSecretId, CancellationToken stoppingToken)
    {
        var apiClientSecretEntity = await context.ApiClientSecrets
            .FirstAsync(x => x.ApiClientSecretId == apiClientSecretId, stoppingToken);

        if (apiClientSecretEntity.RevokedAt.HasValue)
        {
            return;
        }

        apiClientSecretEntity.RevokedAt = timeProvider.GetUtcNow();
        apiClientSecretEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.ApiClientSecrets.Update(apiClientSecretEntity);
    }

    public async Task StampSecretUsedAsync(ApiClientSecretId apiClientSecretId, CancellationToken stoppingToken)
    {
        Guid apiClientSecretIdValue = apiClientSecretId;
        var lastUsedAt = timeProvider.GetUtcNow();

        await context.ApiClientSecrets
            .Where(x => x.ApiClientSecretId == apiClientSecretIdValue)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.LastUsedAt, lastUsedAt), stoppingToken);
    }

    protected override Task<List<ApiClient>> MapPageAsync(List<ApiClientEntity> items, ListApiClientRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<ApiClientEntity> OrderByExpression()
    {
        return new OrderByExpression<ApiClientEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<ApiClientEntity> FilterByPagedRequest(ListApiClientRequest listApiClientRequest)
    {
        var queryable = context.ApiClients
            .Include(x => x.Scopes)
            .Where(x => x.OrganizationId == listApiClientRequest.OrganizationId && !x.IsDeleted);

        if (listApiClientRequest.FriendlyName.HasValue)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.FriendlyName, $"%{listApiClientRequest.FriendlyName}%"));
        }

        return queryable;
    }

    protected override IQueryable<ApiClientEntity> FilterByDateRange(IQueryable<ApiClientEntity> queryable, DatedRequest datedRequest)
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
