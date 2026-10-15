using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Dunning;
using Presentation.Models.Dunning;
using Presentation.Validators.Common;

namespace Presentation.Validators.Dunning;

public class ListDunningStateRequestValidator(
    IOrganizationService organizationService,
    IDunningService dunningService,
    TimeProvider timeProvider) : PagedRequestValidator<ListDunningStateRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.FilterOrganizationId)
            .Cascade(CascadeMode.Stop)
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x!.Value), new IncludeDeleted(false), ct))
            .When(x => x.FilterOrganizationId.HasValue)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListDunningStateRequestDto request, CancellationToken stoppingToken)
    {
        return dunningService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
