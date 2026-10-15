using System.Net;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.CaseLinks;
using Core.Models.Common;
using Core.ValueObjects;
using FluentValidation;
using FluentValidation.Results;

namespace Core.Services;

public class CaseLinkService(
    ICaseLinkRepository caseLinkRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : ICaseLinkService
{
    public async Task<CaseLink> CreateAsync(CreateCaseLinkRequest createCaseLinkRequest, CancellationToken stoppingToken)
    {
        EnforceDistinct(createCaseLinkRequest);
        await EnforceNotAlreadyLinkedAsync(createCaseLinkRequest, stoppingToken);

        var caseLink = createCaseLinkRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await caseLinkRepository.AddAsync(caseLink, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(caseLink.CaseLinkId, createCaseLinkRequest.CaseId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, CaseLinkId caseLinkId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return caseLinkRepository.ExistsAsync(organizationId, caseId, caseLinkId, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<CaseLink>> ListAsync(ListCaseLinkRequest listCaseLinkRequest, CancellationToken stoppingToken)
    {
        return caseLinkRepository.ListAsync(listCaseLinkRequest, stoppingToken);
    }

    public Task<CaseLink> GetAsync(CaseLinkId caseLinkId, CaseId viewpointCaseId, CancellationToken stoppingToken)
    {
        return caseLinkRepository.GetAsync(caseLinkId, viewpointCaseId, stoppingToken);
    }

    public async Task DeleteAsync(CaseLinkId caseLinkId, CancellationToken stoppingToken)
    {
        await caseLinkRepository.DeleteAsync(caseLinkId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListCaseLinkRequest listCaseLinkRequest, CancellationToken stoppingToken)
    {
        return caseLinkRepository.GetCountAsync(listCaseLinkRequest, stoppingToken);
    }

    private static void EnforceDistinct(CreateCaseLinkRequest createCaseLinkRequest)
    {
        if (createCaseLinkRequest.CaseId != createCaseLinkRequest.LinkedCaseId)
        {
            return;
        }

        throw new ValidationException(
        [
            new ValidationFailure(nameof(CaseLink.LinkedCaseId), "A case cannot be linked to itself")
            {
                ErrorCode = HttpStatusCode.Conflict.ToString()
            }
        ]);
    }

    private async Task EnforceNotAlreadyLinkedAsync(CreateCaseLinkRequest createCaseLinkRequest, CancellationToken stoppingToken)
    {
        var exists = await caseLinkRepository.ExistsBetweenAsync(
            createCaseLinkRequest.OrganizationId,
            createCaseLinkRequest.CaseId,
            createCaseLinkRequest.LinkedCaseId,
            stoppingToken);

        if (!exists)
        {
            return;
        }

        throw new ValidationException(
        [
            new ValidationFailure(nameof(CaseLink.LinkedCaseId), "These cases are already linked")
            {
                ErrorCode = HttpStatusCode.Conflict.ToString()
            }
        ]);
    }
}
