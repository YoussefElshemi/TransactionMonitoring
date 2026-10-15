using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.SubOrganizations;
using Core.ValueObjects;

namespace Core.Services;

public class SubOrganizationService(
    ISubOrganizationRepository subOrganizationRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ISubOrganizationService
{
    public async Task<SubOrganization> CreateAsync(CreateSubOrganizationRequest createSubOrganizationRequest, CancellationToken stoppingToken)
    {
        var subOrganization = createSubOrganizationRequest.ToModel(timeProvider);

        await subOrganizationRepository.AddAsync(subOrganization, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(subOrganization.SubOrganizationId, new IncludeDeleted(false), stoppingToken);
    }

    public Task<SubOrganization> GetAsync(SubOrganizationId subOrganizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return subOrganizationRepository.GetAsync(subOrganizationId, includeDeleted, stoppingToken);
    }

    public Task<SubOrganization?> FindByNameAsync(OrganizationId organizationId, SubOrganizationName name, CancellationToken stoppingToken)
    {
        return subOrganizationRepository.FindByNameAsync(organizationId, name, stoppingToken);
    }

    public Task<PagedResponse<SubOrganization>> ListAsync(ListSubOrganizationRequest listSubOrganizationRequest, CancellationToken stoppingToken)
    {
        return subOrganizationRepository.ListAsync(listSubOrganizationRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListSubOrganizationRequest listSubOrganizationRequest, CancellationToken stoppingToken)
    {
        return subOrganizationRepository.GetCountAsync(listSubOrganizationRequest, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, SubOrganizationId subOrganizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return subOrganizationRepository.ExistsAsync(organizationId, subOrganizationId, includeDeleted, stoppingToken);
    }

    public Task<SubOrganizationUsage> GetUsageAsync(SubOrganizationId subOrganizationId, CancellationToken stoppingToken)
    {
        return subOrganizationRepository.GetUsageAsync(subOrganizationId, stoppingToken);
    }

    public async Task UpdateAsync(UpdateSubOrganizationRequest updateSubOrganizationRequest, CancellationToken stoppingToken)
    {
        await subOrganizationRepository.UpdateAsync(updateSubOrganizationRequest, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task DeleteAsync(SubOrganizationId subOrganizationId, CancellationToken stoppingToken)
    {
        await subOrganizationRepository.DeleteAsync(subOrganizationId, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(SubOrganizationId subOrganizationId, CancellationToken stoppingToken)
    {
        await subOrganizationRepository.RecoverAsync(subOrganizationId, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }
}
