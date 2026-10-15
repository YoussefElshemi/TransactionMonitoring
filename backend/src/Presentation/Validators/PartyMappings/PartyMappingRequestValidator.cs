using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.PartyMappings;

namespace Presentation.Validators.PartyMappings;

public class PartyMappingRequestValidator : AbstractValidator<PartyMappingRequestDto>
{
    public PartyMappingRequestValidator(IOrganizationService organizationService, IPartyMappingService partyMappingService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found")
            .MustAsync(async (x, _, ct) => await partyMappingService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Party mapping not found");
    }
}

public class GetPartyMappingRequestValidator : AbstractValidator<GetPartyMappingRequestDto>
{
    public GetPartyMappingRequestValidator(IOrganizationService organizationService, IPartyMappingService partyMappingService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found")
            .MustAsync(async (x, _, ct) => await partyMappingService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(x.IncludeDeleted), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Party mapping not found");
    }
}

public class RecoverPartyMappingRequestValidator : AbstractValidator<RecoverPartyMappingRequestDto>
{
    public RecoverPartyMappingRequestValidator(IOrganizationService organizationService, IPartyMappingService partyMappingService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found")
            .MustAsync(async (x, _, ct) => await partyMappingService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Party mapping not found");
    }
}

public class PartyMappingHistoryRequestValidator : AbstractValidator<PartyMappingHistoryRequestDto>
{
    public PartyMappingHistoryRequestValidator(IOrganizationService organizationService, IPartyMappingService partyMappingService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found")
            .MustAsync(async (x, _, ct) => await partyMappingService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Party mapping not found");
    }
}

public class PartyMappingVersionRequestValidator : AbstractValidator<PartyMappingVersionRequestDto>
{
    public PartyMappingVersionRequestValidator(IOrganizationService organizationService, IPartyMappingService partyMappingService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.VersionNumber)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
            .MustAsync(async (x, _, ct) => await partyMappingService.VersionExistsAsync(new OrganizationId(x.OrganizationId), new VersionNumber(x.VersionNumber), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Party mapping version not found");
    }
}
