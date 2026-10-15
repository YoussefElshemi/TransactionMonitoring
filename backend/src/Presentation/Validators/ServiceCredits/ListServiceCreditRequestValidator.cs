using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.ServiceCredits;
using Presentation.Models.ServiceCredits;
using Presentation.Validators.Common;

namespace Presentation.Validators.ServiceCredits;

public class ListServiceCreditRequestValidator(
    IOrganizationService organizationService,
    IServiceCreditService serviceCreditService,
    TimeProvider timeProvider) : PagedRequestValidator<ListServiceCreditRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListServiceCreditRequestDto request, CancellationToken stoppingToken)
    {
        return serviceCreditService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
