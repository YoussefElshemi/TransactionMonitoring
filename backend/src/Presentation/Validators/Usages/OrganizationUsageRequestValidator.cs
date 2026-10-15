using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Usages;
using Presentation.Models.Usages;
using Presentation.Validators.Common;

namespace Presentation.Validators.Usages;

public class OrganizationUsageRequestValidator : AbstractValidator<OrganizationUsageRequestDto>
{
    public OrganizationUsageRequestValidator(IOrganizationService organizationService, IUsageService usageService)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.UsageId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await usageService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new UsageId(x.UsageId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Usage not found");
    }
}

public class OrganizationListUsageRequestValidator(
    IUsageService usageService,
    IOrganizationService organizationService,
    TimeProvider timeProvider) : PagedRequestValidator<OrganizationListUsageRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }

    protected override Task<TotalCount> GetCountAsync(OrganizationListUsageRequestDto request, CancellationToken stoppingToken)
    {
        return usageService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
