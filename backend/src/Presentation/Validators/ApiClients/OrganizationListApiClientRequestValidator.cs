using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.ApiClients;
using Presentation.Models.ApiClients;
using Presentation.Validators.Common;

namespace Presentation.Validators.ApiClients;

public class OrganizationListApiClientRequestValidator(
    IApiClientService apiClientService,
    IOrganizationService organizationService,
    TimeProvider timeProvider) : PagedRequestValidator<OrganizationListApiClientRequestDto>(timeProvider)
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

    protected override Task<TotalCount> GetCountAsync(OrganizationListApiClientRequestDto request, CancellationToken stoppingToken)
    {
        return apiClientService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
