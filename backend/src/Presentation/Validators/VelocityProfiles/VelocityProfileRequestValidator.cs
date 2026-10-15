using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.VelocityProfiles;

namespace Presentation.Validators.VelocityProfiles;

public class VelocityProfileRequestValidator : AbstractValidator<VelocityProfileRequestDto>
{
    public VelocityProfileRequestValidator(IOrganizationService organizationService, IVelocityProfileService velocityProfileService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found")
            .MustAsync(async (x, _, ct) => await velocityProfileService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Velocity profile not found");
    }
}

public class GetVelocityProfileRequestValidator : AbstractValidator<GetVelocityProfileRequestDto>
{
    public GetVelocityProfileRequestValidator(IOrganizationService organizationService, IVelocityProfileService velocityProfileService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found")
            .MustAsync(async (x, _, ct) => await velocityProfileService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(x.IncludeDeleted), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Velocity profile not found");
    }
}

public class RecoverVelocityProfileRequestValidator : AbstractValidator<RecoverVelocityProfileRequestDto>
{
    public RecoverVelocityProfileRequestValidator(IOrganizationService organizationService, IVelocityProfileService velocityProfileService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found")
            .MustAsync(async (x, _, ct) => await velocityProfileService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Velocity profile not found");
    }
}

public class VelocityProfileHistoryRequestValidator : AbstractValidator<VelocityProfileHistoryRequestDto>
{
    public VelocityProfileHistoryRequestValidator(IOrganizationService organizationService, IVelocityProfileService velocityProfileService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found")
            .MustAsync(async (x, _, ct) => await velocityProfileService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Velocity profile not found");
    }
}

public class ListVelocityBackfillRequestValidator : AbstractValidator<ListVelocityBackfillRequestDto>
{
    public ListVelocityBackfillRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }
}

public class ListMyVelocityBackfillRequestValidator : AbstractValidator<ListMyVelocityBackfillRequestDto>
{
    public ListMyVelocityBackfillRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }
}
