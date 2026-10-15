using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.PartyMappings;
using Presentation.Models.PartyMappings;
using Presentation.Validators.Common;

namespace Presentation.Validators.PartyMappings;

public class ListPartyMappingVersionRequestValidator(
    IPartyMappingService partyMappingService,
    IOrganizationService organizationService,
    TimeProvider timeProvider)
    : PagedRequestValidator<ListPartyMappingVersionRequestDto>(timeProvider)
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

    protected override Task<TotalCount> GetCountAsync(ListPartyMappingVersionRequestDto request, CancellationToken stoppingToken)
    {
        return partyMappingService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}

public class ListMyPartyMappingVersionRequestValidator(
    IPartyMappingService partyMappingService,
    IOrganizationService organizationService,
    TimeProvider timeProvider)
    : PagedRequestValidator<ListMyPartyMappingVersionRequestDto>(timeProvider)
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

    protected override Task<TotalCount> GetCountAsync(ListMyPartyMappingVersionRequestDto request, CancellationToken stoppingToken)
    {
        return partyMappingService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
