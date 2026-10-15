using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Parties;
using Presentation.Models.Parties;
using Presentation.Validators.Common;

namespace Presentation.Validators.Parties;

public class PartyRequestValidator : AbstractValidator<PartyRequestDto>
{
    public PartyRequestValidator(IPartyService partyService)
    {
        RuleFor(x => x.PartyId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await partyService.ExistsAsync(new OrganizationId(x.OrganizationId), new PartyId(x.PartyId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Party not found");
    }
}

public class ListPartyRequestValidator(
    IPartyService partyService,
    IOrganizationService organizationService,
    TimeProvider timeProvider)
    : PagedRequestValidator<ListPartyRequestDto>(timeProvider)
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

        RuleFor(x => x.Role)
            .IsEnumName(typeof(PartyRole), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Role));

        RuleFor(x => x.Query)
            .Must(SearchQuery.IsValid)
            .WithMessage($"A query is between {SearchQuery.MinimumLength} and {SearchQuery.MaximumLength} characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Query));
    }

    protected override Task<TotalCount> GetCountAsync(ListPartyRequestDto request, CancellationToken stoppingToken)
    {
        return partyService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}

public class ListPartyTransactionRequestValidator(
    IPartyService partyService,
    IOrganizationService organizationService,
    TimeProvider timeProvider)
    : PagedRequestValidator<ListPartyTransactionRequestDto>(timeProvider)
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

        RuleFor(x => x.PartyId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await partyService.ExistsAsync(new OrganizationId(x.OrganizationId), new PartyId(x.PartyId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Party not found");

        RuleFor(x => x.Role)
            .IsEnumName(typeof(PartyRole), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Role));
    }

    protected override Task<TotalCount> GetCountAsync(ListPartyTransactionRequestDto request, CancellationToken stoppingToken)
    {
        return partyService.GetTransactionCountAsync(request.ToModel(), stoppingToken);
    }
}

public class ListPartyCaseRequestValidator(
    IPartyService partyService,
    IOrganizationService organizationService,
    TimeProvider timeProvider)
    : PagedRequestValidator<ListPartyCaseRequestDto>(timeProvider)
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

        RuleFor(x => x.PartyId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await partyService.ExistsAsync(new OrganizationId(x.OrganizationId), new PartyId(x.PartyId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Party not found");

        RuleFor(x => x.Status)
            .IsEnumName(typeof(CaseStatus), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Status));
    }

    protected override Task<TotalCount> GetCountAsync(ListPartyCaseRequestDto request, CancellationToken stoppingToken)
    {
        return partyService.GetCaseCountAsync(request.ToModel(), stoppingToken);
    }
}
