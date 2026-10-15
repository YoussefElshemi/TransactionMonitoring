using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.Organizations;
using Core.ValueObjects;

namespace Core.Services;

public class OrganizationService(
    IOrganizationRepository organizationRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IOrganizationService
{
    public async Task<Organization> CreateAsync(CreateOrganizationRequest createOrganizationRequest, CancellationToken stoppingToken)
    {
        var organization = createOrganizationRequest.ToModel(timeProvider);

        await organizationRepository.AddAsync(organization, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(organization.OrganizationId, stoppingToken);
    }

    public async Task UpdateAsync(UpdateOrganizationRequest updateOrganizationRequest, CancellationToken stoppingToken)
    {
        await organizationRepository.UpdateAsync(updateOrganizationRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return organizationRepository.ExistsAsync(organizationId, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<Organization>> ListAsync(ListOrganizationRequest listOrganizationRequest, CancellationToken stoppingToken)
    {
        return organizationRepository.ListAsync(listOrganizationRequest, stoppingToken);
    }

    public Task<Organization> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        return organizationRepository.GetAsync(organizationId, stoppingToken);
    }

    public Task<List<Organization>> ListByIdsAsync(IReadOnlyCollection<OrganizationId> organizationIds, CancellationToken stoppingToken)
    {
        return organizationRepository.ListByIdsAsync(organizationIds, stoppingToken);
    }

    public Task<List<Organization>> GetAllAsync(CancellationToken stoppingToken)
    {
        return organizationRepository.GetAllAsync(stoppingToken);
    }

    public async Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        await organizationRepository.DeleteAsync(organizationId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        await organizationRepository.RecoverAsync(organizationId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListOrganizationRequest listOrganizationRequest, CancellationToken stoppingToken)
    {
        return organizationRepository.GetCountAsync(listOrganizationRequest, stoppingToken);
    }
}