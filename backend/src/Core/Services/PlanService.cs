using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.Plans;
using Core.ValueObjects;

namespace Core.Services;

public class PlanService(
    IPlanRepository planRepository,
    IPlanVersionRepository planVersionRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IPlanService
{
    public async Task<Plan> CreateAsync(CreatePlanRequest createPlanRequest, CancellationToken stoppingToken)
    {
        PriceStructureValidator.Validate(createPlanRequest.Prices);

        var planId = new PlanId(Guid.NewGuid());

        var plan = createPlanRequest.ToModel(planId, timeProvider);
        var planVersion = createPlanRequest.ToVersionModel(planId, timeProvider);

        await planRepository.AddAsync(plan, stoppingToken);
        await planVersionRepository.AddAsync(planVersion, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(planId, stoppingToken);
    }

    public Task<Plan> GetAsync(PlanId planId, CancellationToken stoppingToken)
    {
        return planRepository.GetAsync(planId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(PlanId planId, CancellationToken stoppingToken)
    {
        return planRepository.ExistsAsync(planId, stoppingToken);
    }

    public Task<Exists> ExistsByNameAsync(PlanName name, OrganizationId? organizationId, CancellationToken stoppingToken)
    {
        return planRepository.ExistsByNameAsync(name, organizationId, stoppingToken);
    }

    public Task<PagedResponse<Plan>> ListAsync(ListPlanRequest listPlanRequest, CancellationToken stoppingToken)
    {
        return planRepository.ListAsync(listPlanRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListPlanRequest listPlanRequest, CancellationToken stoppingToken)
    {
        return planRepository.GetCountAsync(listPlanRequest, stoppingToken);
    }

    public async Task<Plan> UpdateAsync(UpdatePlanRequest updatePlanRequest, CancellationToken stoppingToken)
    {
        await planRepository.UpdateAsync(updatePlanRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(updatePlanRequest.PlanId, stoppingToken);
    }

    public async Task DeleteAsync(PlanId planId, CancellationToken stoppingToken)
    {
        var plan = await GetAsync(planId, stoppingToken);

        if (await planRepository.HasSubscriptionsAsync(planId, stoppingToken))
        {
            throw new PlanInUseException(plan.Name);
        }

        await planRepository.DeleteAsync(planId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task<PlanVersion> PublishVersionAsync(PublishPlanVersionRequest publishPlanVersionRequest, CancellationToken stoppingToken)
    {
        PriceStructureValidator.Validate(publishPlanVersionRequest.Prices);

        var versionNumber = await planVersionRepository.GetNextVersionNumberAsync(publishPlanVersionRequest.PlanId, stoppingToken);
        var planVersion = publishPlanVersionRequest.ToModel(versionNumber, timeProvider);

        await planVersionRepository.AddAsync(planVersion, stoppingToken);

        await planRepository.SetCurrentVersionAsync(publishPlanVersionRequest.PlanId, versionNumber, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return planVersion;
    }

    public Task<PlanVersion> GetVersionAsync(PlanVersionId planVersionId, CancellationToken stoppingToken)
    {
        return planVersionRepository.GetAsync(planVersionId, stoppingToken);
    }

    public Task<Exists> VersionExistsAsync(PlanVersionId planVersionId, CancellationToken stoppingToken)
    {
        return planVersionRepository.ExistsAsync(planVersionId, stoppingToken);
    }

    public Task<PagedResponse<PlanVersion>> ListVersionsAsync(ListPlanVersionRequest listPlanVersionRequest, CancellationToken stoppingToken)
    {
        return planVersionRepository.ListAsync(listPlanVersionRequest, stoppingToken);
    }

    public Task<TotalCount> GetVersionCountAsync(ListPlanVersionRequest listPlanVersionRequest, CancellationToken stoppingToken)
    {
        return planVersionRepository.GetCountAsync(listPlanVersionRequest, stoppingToken);
    }
}
